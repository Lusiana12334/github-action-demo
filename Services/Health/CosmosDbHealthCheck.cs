using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using PEXC.Case.DataAccess.CosmosDB;

namespace PEXC.Case.Services.Health;

public class CosmosDbHealthCheck : PexcHealthCheck
{
    public const string HealthCheckName = "Cosmos DB";

    private readonly ICosmosDbRepository _cosmosDbRepository;

    public CosmosDbHealthCheck(ICosmosDbRepository cosmosDbRepository, ILogger<CosmosDbHealthCheck> logger)
        : base(logger)
        => _cosmosDbRepository = cosmosDbRepository;

    public override string Name => HealthCheckName;

    protected override async Task<HealthCheckResult> GetHealthStatus(
        HealthCheckContext context,
        CancellationToken cancellationToken = new())
        => await _cosmosDbRepository.CanConnect()
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Could not connect to the Cosmos database.");
}