using Microsoft.Extensions.Logging;
using PEXC.Case.DataAccess;
using PEXC.Case.Domain;

namespace PEXC.Case.Services;

public class PerformanceTestCaseService : IPerformanceTestCaseService
{
    public static readonly string ServiceUserName = "Delete Performance Test Cases Service";


    private readonly ILogger<PerformanceTestCaseService> _logger;
    private readonly IPerformanceTestCaseRepository _repository;

    public PerformanceTestCaseService(
        ILogger<PerformanceTestCaseService> logger,
        IPerformanceTestCaseRepository caseRepository)
    {
        _logger = logger;
        _repository = caseRepository;
    }

    public async Task DeleteCases(Guid correlationId, int databaseQueryPageSize)
    {
        try
        {
            PagedResult<CaseEntity>? caseEntities = null;
            do
            {
                caseEntities = await _repository.GetCasesCreatedByPerformanceTests(databaseQueryPageSize);
                foreach (var caseEntity in caseEntities.Items)
                {
                    await DeleteCase(caseEntity, correlationId);
                }
            }
            while (caseEntities.Items.Count >= databaseQueryPageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
        }
    }

    private async Task DeleteCase(CaseEntity caseEntity, Guid correlationId)
    {
        try
        {
            await _repository.DeleteCaseDocument(caseEntity.Id, caseEntity.Key, correlationId);            
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to delete case. Id: {caseEntity.Id} Key: {caseEntity.Key}", ex);
            throw new ApplicationException($"Stopping performance test cases deletion.", ex);
        }
    }
}