using System.Linq.Expressions;
using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using PEXC.Case.Domain;

namespace PEXC.Case.DataAccess.CosmosDB;

internal class CosmosSingleCaseRepository : ISingleCaseRepository
{
    private readonly ILogger<CosmosSingleCaseRepository> _logger;

    private readonly ICosmosDbRepository _cosmosDbRepository;

    public CosmosSingleCaseRepository(
        ICosmosDbRepository cosmosDbRepository, 
        ILogger<CosmosSingleCaseRepository> logger)
    {
        _cosmosDbRepository = cosmosDbRepository;
        _logger = logger;
    }

    public virtual async Task<CaseEntity?> GetNonRetainerCaseByCaseCode(string caseCode)
    {
        Expression<Func<CaseEntity, bool>> nonRetainerPredicate =
            caseEntity =>
                caseEntity.Type == nameof(CaseEntity) &&
                caseEntity.CaseCode == caseCode &&
                caseEntity.RelationshipType == RelationshipType.NonRetainer;

        var results = (await _cosmosDbRepository
                .Query(predicate: nonRetainerPredicate, partitionKey: caseCode, includeDeletedItems: true))
            .Items;

        if (results.Count > 1)
        {
            _logger.LogError("More than one case for non-retainer case {caseCode}. Ids : {iDs} !! update will be performed only on first ! ",
                caseCode, string.Join(";", results.Select(item => item.Id)));
        }

        return results.FirstOrDefault();
    }

    public async Task<CaseEntity?> GetRetainerCaseByCaseCodeAndName(string caseCode, string caseName)
    {
        Expression<Func<CaseEntity, bool>> retainerPredicate =
            caseEntity =>
                caseEntity.Type == nameof(CaseEntity) &&
                caseEntity.CaseCode == caseCode &&
                caseEntity.CaseName == caseName &&
                caseEntity.RelationshipType == RelationshipType.Retainer;

        return (await _cosmosDbRepository.Query(predicate: retainerPredicate, includeDeletedItems: true)).Items
            .SingleOrDefault();
    }

    public virtual Task<CaseEntity?> GetCase(string caseId, string key)
        => _cosmosDbRepository.GetDocument<CaseEntity>(caseId, key);

    public Task<CaseEntity> AddCase(CaseEntity caseEntity)
        => _cosmosDbRepository.CreateDocument(caseEntity);

    public async Task<bool> UpdateCase(CaseEntity caseEntity)
    {
        if (!await Exists(caseEntity.Id, caseEntity.Key))
        {
            throw new CosmosException(
                $"Case {caseEntity.Id} / {caseEntity.Key} does not exist!",
                HttpStatusCode.NotFound,
                0,
                string.Empty,
                0);
        }

        await _cosmosDbRepository.UpsertDocument(caseEntity);
        return true;
    }

    public async Task<bool> PatchCase(string caseId, string key, IReadOnlyDictionary<string, object?> propertiesToUpdate)
    {
        await _cosmosDbRepository
            .PatchDocument<CaseEntity>(caseId, key, propertiesToUpdate);

        return true;
    }

    protected virtual async Task<bool> Exists(string caseId, string key)
        => await _cosmosDbRepository.GetDocument<CaseEntity>(caseId, key) is not null;
}