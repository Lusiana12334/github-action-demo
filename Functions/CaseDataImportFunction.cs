using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using PEXC.Case.Services.CCM;

namespace PEXC.Case.Functions;

public class CaseDataImportFunction
{
    private readonly ICaseDataImportService _caseDataImportService;
    private readonly ILogger<CaseDataImportFunction> _logger;

    public CaseDataImportFunction(
        ICaseDataImportService caseDataImportService,
        ILogger<CaseDataImportFunction> logger)
    {
        _caseDataImportService = caseDataImportService ?? throw new ArgumentNullException(nameof(caseDataImportService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Timeout("24:00:00")]
    [FunctionName(nameof(ImportCaseDataManualTrigger))]
    public async Task<IActionResult> ImportCaseDataManualTrigger(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
        HttpRequest req)
    {
        _logger.LogInformation("Case Data Import from CCM started...");
        await _caseDataImportService.ImportCases();
        _logger.LogInformation("Case Data Import from CCM finished.");
        return new OkObjectResult("OK");
    }

    [Timeout("24:00:00")]
    [FunctionName(nameof(ImportCaseData))]
    public async Task ImportCaseData(
        [TimerTrigger("%CaseDataImportOptions:RefreshSchedule%")]
        TimerInfo timerInfo)
    {
        _logger.LogInformation("Case Data Import from CCM started...");
        await _caseDataImportService.ImportCases();
        _logger.LogInformation("Case Data Import from CCM finished.");
    }
}