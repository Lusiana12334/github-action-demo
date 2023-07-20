using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace PEXC.Case.Functions;

public class HealthCheckFunction
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<HealthCheckFunction> _logger;

    public HealthCheckFunction(HealthCheckService healthCheckService, ILogger<HealthCheckFunction> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    [FunctionName(nameof(HealthCheckFunction))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest req)
    {
        var healthReport = await _healthCheckService.CheckHealthAsync();
        _logger.LogInformation("Health status for Case Function App: {healthStatus}", healthReport.Status);
        return
            new ObjectResult(healthReport.Status.ToString())
            {
                StatusCode = healthReport.Status is HealthStatus.Healthy or HealthStatus.Degraded
                    ? StatusCodes.Status200OK
                    : StatusCodes.Status503ServiceUnavailable
            };
    }
}