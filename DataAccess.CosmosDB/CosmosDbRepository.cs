using System.Linq.Expressions;
using System.Net;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using PEXC.Case.DataAccess.CosmosDB.Infrastructure;
using PEXC.Case.Domain;
using PEXC.Common.Logging.Extensions;
using Polly;

namespace PEXC.Case.DataAccess.CosmosDB;

public class CosmosDbRepository : ICosmosDbRepository
{
    private readonly CosmosClient _cosmosClient;
    private readonly IAsyncPolicy _asyncPolicy;
    private readonly string _database;
    private readonly string _containerName;
    private readonly bool _populateIndexMetrics;
    private readonly ILogger<CosmosDbRepository> _logger;

    public CosmosDbRepository(
        CosmosClient cosmosClient,
        IAsyncPolicy asyncPolicy,
        string database,
        string containerName,
        bool populateIndexMetrics,
        ILogger<CosmosDbRepository> logger)
    {
        _cosmosClient = cosmosClient;
        _asyncPolicy = asyncPolicy;
        _database = database;
        _containerName = containerName;
        _populateIndexMetrics = populateIndexMetrics;
        _logger = logger;
    }

    public async Task<T> CreateDocument<T>(T document) where T : IEntity
    {
        document.CorrelationId = Guid.NewGuid();

        using var _ = _logger.BeginScope(document.CorrelationId.ToString(), document.Key, document.Id);

        _logger.LogInformation("Creating document {document},", document);

        var container = GetContainer();

        var createdDocument = await _asyncPolicy.ExecuteAsync(
            _ => container.CreateItemAsync(document, new PartitionKey(document.Key)),
            GetPollyContext());

        _logger.LogInformation("Document created. Status: {statusCode}. {rus} RUs", createdDocument.StatusCode, createdDocument.RequestCharge);
        return createdDocument;
    }

    public async Task<T> PatchDocument<T>(string id, string partitionKey, IReadOnlyDictionary<string, object?> propertiesToUpdate) where T : IEntity
    {
        var correlationIdKey = JsonNamingPolicy.CamelCase.ConvertName(nameof(IEntity.CorrelationId));

        var correlationId = propertiesToUpdate.GetValueOrDefault(correlationIdKey) ?? Guid.NewGuid();

        using var _ = _logger.BeginScope(correlationId.ToString(), partitionKey, id);

        _logger.LogInformation("Patching document {props}",
            string.Join(';', propertiesToUpdate.Select(item => $"[{item.Key} - {item.Value}]")));

        var container = GetContainer();
        var patchOperations = propertiesToUpdate
            .Where(item => item.Key != correlationIdKey)
            .Select(item =>
                PatchOperation.Set($"/{item.Key}", item.Value)).ToList();

        patchOperations.Add(PatchOperation.Add($"/{correlationIdKey}", correlationId));

        var result = await _asyncPolicy.ExecuteAsync(
            async _ => await container.PatchItemAsync<T>(id, new PartitionKey(partitionKey), patchOperations),
            GetPollyContext());

        _logger.LogInformation("Document patched, {rus} RUs",  result.RequestCharge);
        return result;
    }

    public async Task<T> UpsertDocument<T>(T document) where T : IEntity
    {
        document.CorrelationId = Guid.NewGuid();

        using var _ = _logger.BeginScope(document.CorrelationId.ToString(), document.Key, document.Id,
            new Dictionary<string, object>
                { { nameof(IEntity.Type), document.Type } });

        _logger.LogInformation("Upserting document {document}", document);
        var container = GetContainer();
        var upsertDocument = await _asyncPolicy.ExecuteAsync(
            _ => container.UpsertItemAsync(document, new PartitionKey(document.Key)),
            GetPollyContext());
        _logger.LogInformation("Document upserted. Status: {statusCode}, used {rus} RUs",
            upsertDocument.StatusCode, upsertDocument.RequestCharge);
        return upsertDocument;
    }

    public async Task<T> HardDeleteDocument<T>(string id, string partitionKey, Guid correlationId) where T : IEntity
    {
        using var _ = _logger.BeginScope(correlationId.ToString(), partitionKey, id);

        _logger.LogInformation("Deleting document id: {id}, key: {key}", id, partitionKey);
        var container = GetContainer();
        var deleteDocument = await _asyncPolicy.ExecuteAsync(
            _ => container.DeleteItemAsync<T>(id, new PartitionKey(partitionKey)),
            GetPollyContext());
        _logger.LogInformation("Document deleted. Status: {statusCode}, used {rus} RUs",
            deleteDocument.StatusCode, deleteDocument.RequestCharge);
        return deleteDocument;
    }

    public async Task<T?> GetDocument<T>(string id, string partitionKey) where T : IEntity
    {
        using var _ = _logger.BeginScope(
            new Dictionary<string, object>
                { { nameof(IEntity.Key), partitionKey }, { nameof(IEntity.Id), id } });

        _logger.LogInformation("Fetching document");
        var container = GetContainer();

        try
        {
            var document = await _asyncPolicy.ExecuteAsync(
                _ => container.ReadItemAsync<T>(id, new(partitionKey)),
                GetPollyContext());

            _logger.LogInformation("Document fetched, {rus} RUs, {correlationId}",  document.RequestCharge, document.Resource.CorrelationId);
            return document;
        }
        catch (CosmosException cosmosException) when (cosmosException.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogInformation("Document not found");
            return default;
        }
    }

    public async Task<PagedResult<TResult>> Query<T, TResult>(
        Expression<Func<T, TResult>> selector,
        Expression<Func<T, bool>>? predicate = null,
        int? pageSize = null,
        string? nextPageToken = null,
        string? partitionKey = null,
        bool includeDeletedItems = false
        ) where T : IEntity
    {
        nextPageToken = nextPageToken.FromBase64();
        _logger.LogInformation("Querying for entities with PK {pk} with predicate {predicate}", partitionKey, predicate);
        var container = GetContainer();
        var requestOptions = new QueryRequestOptions
        {
            PartitionKey = string.IsNullOrEmpty(partitionKey) ? null : new PartitionKey(partitionKey),
            MaxItemCount = pageSize,
            ResponseContinuationTokenLimitInKb = 1,
            PopulateIndexMetrics = _populateIndexMetrics
        };

        if (!includeDeletedItems && typeof(T).Name == nameof(CaseEntity))
        {
            ParameterExpression caseParam = predicate?.Parameters[0] ?? Expression.Parameter(typeof(CaseEntity), "c");
            var itemStageProperty = Expression.Property(caseParam, nameof(CaseEntity.ItemStage));

            predicate = Expression.Lambda<Func<T, bool>>(
                predicate != null
                    ? Expression.AndAlso(predicate.Body,
                        Expression.NotEqual(itemStageProperty, Expression.Constant(CaseState.Deleted)))
                    : Expression.NotEqual(itemStageProperty, Expression.Constant(CaseState.Deleted)),
                caseParam);
        }

        var results = new List<TResult>();
        var queryCost = 0.0;
        var queryText = string.Empty;
        do
        {
            IQueryable<T> baseQuery = container
                .GetItemLinqQueryable<T>(continuationToken: nextPageToken, requestOptions: requestOptions);
            if (predicate != null)
            {
                baseQuery = baseQuery.Where(predicate);
                queryText = baseQuery.ToQueryDefinition().QueryText;
            }

            var entitiesQuery = baseQuery.Select(selector);

            using var feedIterator = entitiesQuery.ToFeedIterator();

            nextPageToken = null;
            if (!feedIterator.HasMoreResults) break;

            var feedResponse = await _asyncPolicy.ExecuteAsync(
                _ => feedIterator.ReadNextAsync(),
                GetPollyContext());
            results.AddRange(feedResponse);
            queryCost += feedResponse.RequestCharge;

            if (_populateIndexMetrics) _logger.LogDebug("Cosmos DB Index metrics: {indexMetrics}", feedResponse.IndexMetrics);

            if (feedResponse.ContinuationToken == null) break;

            nextPageToken = feedResponse.ContinuationToken;
            requestOptions.MaxItemCount = pageSize.HasValue ? pageSize.Value - results.Count : null;
        } while (!pageSize.HasValue || results.Count < pageSize);

        _logger.LogInformation(
            "Queried for entities with PK {pk} with query \"{query}\", amount of results {count}, has next page: {hasNextPage}, {rus} RUs. Correlation ids {correlationIds}",
            partitionKey,
            queryText,
            results.Count,
            nextPageToken == null ? "No" : "Yes",
            queryCost,
            string.Join(';', results.OfType<IEntity>().Select(item => item.CorrelationId)));

        return new PagedResult<TResult>
        {
            Items = results,
            NextPageToken = nextPageToken.ToBase64()
        };
    }

    public async Task<bool> CanConnect()
    {
        try
        {
            await _cosmosClient.ReadAccountAsync();
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogErrorWithTelemetry(ex, "Could not connect to the Cosmos DB.");
            return false;
        }
    }

    private Container GetContainer()
        => _cosmosClient.GetContainer(_database, _containerName);

    private Context GetPollyContext() => new("cosmos", new Dictionary<string, object> { { "logger", _logger } });
}