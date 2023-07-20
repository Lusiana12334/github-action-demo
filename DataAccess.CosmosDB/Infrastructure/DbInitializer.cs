using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PEXC.Case.Domain;

namespace PEXC.Case.DataAccess.CosmosDB.Infrastructure;

public class DbInitializer : IDbInitializer
{
    private readonly CosmosOptions _options;
    private readonly ILogger<DbInitializer> _logger;

    public DbInitializer(IOptions<CosmosOptions> options, ILogger<DbInitializer> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task EnsureIsCreated()
    {
        _logger.LogInformation("Trying to ensure if DB and container are there ..");
        var cosmosBuilderClient = new CosmosClientBuilder(_options.ConnectionString).Build();

        if (_options.CreateDatabase)
        {
            await CreateDatabase(cosmosBuilderClient, _options.Container, $"/{nameof(IEntity.Key).ToLower()}");
            await CreateDatabase(cosmosBuilderClient, _options.AuditContainer, $"/{nameof(IEntity.Key).ToLower()}");
            await CreateDatabase(cosmosBuilderClient, _options.LeasesContainer, "/id");
        }
        else
        {
            CheckIfDatabaseCreated(cosmosBuilderClient, _options.Container);
            CheckIfDatabaseCreated(cosmosBuilderClient, _options.AuditContainer);
            CheckIfDatabaseCreated(cosmosBuilderClient, _options.LeasesContainer);
        }

        _logger.LogInformation("DB and container are working..");
    }

    private void CheckIfDatabaseCreated(CosmosClient cosmosBuilderClient, string containerName)
    {
        var container = cosmosBuilderClient.GetContainer(_options.Database, containerName);
        if (container is null)
        {
            throw new ArgumentException($"No container {containerName} for {_options.Database}");
        }
    }

    private async Task CreateDatabase(CosmosClient cosmosBuilderClient, string containerName, string partitionKey)
    {
        Database database = await cosmosBuilderClient.CreateDatabaseIfNotExistsAsync(_options.Database);
        await database.CreateContainerIfNotExistsAsync(
            new ContainerProperties(containerName, partitionKey));
    }
}