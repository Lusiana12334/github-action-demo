using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using PEXC.Case.Services.IRIS;

namespace PEXC.Case.Functions;

public class IrisDataUpdateFunction
{
    private readonly IIrisDataImportService _irisDataImportService;
    private readonly ILogger<IrisDataUpdateFunction> _logger;

    public IrisDataUpdateFunction(
        IIrisDataImportService irisDataImportService,
        ILogger<IrisDataUpdateFunction> logger)
    {
        _irisDataImportService = irisDataImportService;
        _logger = logger;
    }

    [FunctionName(nameof(UpdateIrisCaseData))]
    public async Task UpdateIrisCaseData(
        [TimerTrigger("%UpdateIrisCaseDataOptions:RefreshSchedule%")]
            TimerInfo timerInfo)
    {
        _logger.LogInformation("Case Data Update from IRIS started...");
        await _irisDataImportService.UpdateCases();
        _logger.LogInformation("Case Data Update from IRIS finished.");
    }

    [FunctionName(nameof(UpdateIrisDataManualTrigger))]
    public async Task<IActionResult> UpdateIrisDataManualTrigger(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
        HttpRequest req, ILogger log)
    {
        _logger.LogInformation("Case Data Update from IRIS started...");
        await _irisDataImportService.UpdateCases();
        _logger.LogInformation("Case Data Update from IRIS finished.");
        return new OkObjectResult("OK");
    }
}