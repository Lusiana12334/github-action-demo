using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using PEXC.Case.Domain;

namespace PEXC.Case.DataAccess.CosmosDB;

internal class CachedSingleCaseRepository : CosmosSingleCaseRepository
{
    private readonly ConcurrentDictionary<string, CaseEntity> _cache = new();

    public CachedSingleCaseRepository(
        ICosmosDbRepository cosmosDbRepository, 
        ILogger<CosmosSingleCaseRepository> logger)
        : base(cosmosDbRepository, logger)
    {  }

    public override async Task<CaseEntity?> GetCase(string caseId, string key)
    {
        if (_cache.TryGetValue(caseId, out var entity))
            return entity;

        entity = await base.GetCase(caseId, key);
        _cache.TryAdd(caseId, entity!);
        return entity;
    }

    protected override async Task<bool> Exists(string caseId, string key)
    {
        if (_cache.TryGetValue(caseId, out _))
            return true;

        return await base.Exists(caseId, key);
    }

    public override async Task<CaseEntity?> GetNonRetainerCaseByCaseCode(string caseCode)
    {
        var result = await base.GetNonRetainerCaseByCaseCode(caseCode);

        if (result != null)
            _cache.TryAdd(result.Id, result);

        return result;
    }
}