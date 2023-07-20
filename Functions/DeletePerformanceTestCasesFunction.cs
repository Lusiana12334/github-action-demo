using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using PEXC.Case.Services;

namespace PEXC.Case.Functions;

public class DeletePerformanceTestCasesFunction
{
    private readonly IPerformanceTestCaseService _service;
    private readonly ILogger<DeletePerformanceTestCasesFunction> _logger;

    private const int databaseQueryPageSize = 100;

    public DeletePerformanceTestCasesFunction(
        IPerformanceTestCaseService? deletePerformanceTestCasesService,
        ILogger<DeletePerformanceTestCasesFunction>? logger)
    {
        _service = deletePerformanceTestCasesService ?? throw new ArgumentNullException(nameof(deletePerformanceTestCasesService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Timeout("24:00:00")]
    [FunctionName(nameof(DeletePerformanceTestCasesManualTrigger))]
    public async Task<IActionResult> DeletePerformanceTestCasesManualTrigger(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
        HttpRequest req)
    {
        var correlationId = Guid.NewGuid();
        _logger.LogInformation("Delete Performance Test Cases Function started...");
        await _service.DeleteCases(correlationId, databaseQueryPageSize);
        _logger.LogInformation("Delete Performance Test Cases Function finished.");
        return new OkObjectResult("OK");
    }
}