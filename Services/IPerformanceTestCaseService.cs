namespace PEXC.Case.Services;

public interface IPerformanceTestCaseService
{
    Task DeleteCases(Guid correlationId, int databaseQueryPageSize);
}