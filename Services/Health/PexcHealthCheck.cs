using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace PEXC.Case.Services.Health;

public abstract class PexcHealthCheck : IHealthCheck
{
    protected readonly ILogger<PexcHealthCheck> Logger;

    protected PexcHealthCheck(ILogger<PexcHealthCheck> logger) => Logger = logger;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new())
    {
        Logger.LogInformation("Running {healthCheckName} Health Check...", Name);

        var healthCheckResult = await GetHealthStatus(context, cancellationToken);

        Logger.LogInformation("{healthCheckName} Health Status: {healthStatus}", Name, healthCheckResult.Status);
        return healthCheckResult;
    }

    public abstract string Name { get; }

    protected abstract Task<HealthCheckResult> GetHealthStatus(
        HealthCheckContext context,
        CancellationToken cancellationToken = new());
}