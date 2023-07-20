using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using PEXC.Case.Services;

namespace PEXC.Case.Functions;

public class SearchableCasesCrawlerFunction
{
    private readonly ICaseSearchabilityService _caseService;
    private readonly ILogger<SearchableCasesCrawlerFunction> _logger;

    public SearchableCasesCrawlerFunction(
        ICaseSearchabilityService? caseService,
        ILogger<SearchableCasesCrawlerFunction>? logger)
    {
        _caseService = caseService ?? throw new ArgumentNullException(nameof(caseService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Timeout("24:00:00")]
    [FunctionName(nameof(CheckSearchableCasesManualTrigger))]
    public async Task<IActionResult> CheckSearchableCasesManualTrigger(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
        HttpRequest req)
    {
        _logger.LogInformation("Searchable Cases Crawler started...");
        await _caseService.UpdateCasesSearchability();
        _logger.LogInformation("Searchable Cases Crawler finished.");
        return new OkObjectResult("OK");
    }

    [Timeout("24:00:00")]
    [FunctionName(nameof(CheckSearchableCases))]
    public async Task CheckSearchableCases(
        [TimerTrigger("%SearchableCasesCrawlerOptions:TriggerSchedule%")]
        TimerInfo timerInfo)
    {
        _logger.LogInformation("Searchable Cases Crawler started...");
        await _caseService.UpdateCasesSearchability();
        _logger.LogInformation("Searchable Cases Crawler finished.");
    }
}