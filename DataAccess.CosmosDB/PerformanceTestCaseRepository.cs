using System.Collections.Concurrent;
using Microsoft.Azure.Cosmos;
using System.Linq.Expressions;
using System.Net;
using Microsoft.Extensions.Logging;
using PEXC.Case.Domain;

namespace PEXC.Case.DataAccess.CosmosDB;

internal class PerformanceTestCaseRepository : IPerformanceTestCaseRepository
{
    private readonly ILogger<PerformanceTestCaseRepository> _logger;

    private readonly ICosmosDbRepository _cosmosDbRepository;

    public PerformanceTestCaseRepository(
        ICosmosDbRepository cosmosDbRepository,
        ILogger<PerformanceTestCaseRepository> logger)
    {
        _cosmosDbRepository = cosmosDbRepository;
        _logger = logger;
    }

    public async Task<bool> DeleteCaseDocument(string caseId, string key, Guid correlationId)
    {
        await _cosmosDbRepository.HardDeleteDocument<CaseEntity>(caseId, key, correlationId);
        return true;
    }

    public Task<PagedResult<CaseEntity>> GetCasesCreatedByPerformanceTests(int? pageSize = null, string? nextPageToken = null)
    {
        Expression<Func<CaseEntity, bool>> casesCreatedByPerformanceTests = 
            caseEntity => caseEntity.Type == nameof(CaseEntity) && caseEntity.Key.StartsWith("PERF_");

        return _cosmosDbRepository.Query(predicate: casesCreatedByPerformanceTests, pageSize, nextPageToken);
    }
}