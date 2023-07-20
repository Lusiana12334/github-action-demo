using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using PEXC.Case.DataAccess.CosmosDB;
using PEXC.Case.Services.Health;

namespace PEXC.Case.Services.Tests.Health;

public class CosmosDbHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenCanConnectRepository_ReturnsHealthyStatus()
    {
        // Arrange
        var repository = Substitute.For<ICosmosDbRepository>();
        repository
            .CanConnect()
            .Returns(true);
        var healthCheck = new CosmosDbHealthCheck(repository, Substitute.For<ILogger<CosmosDbHealthCheck>>());

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status
            .Should()
            .Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenCannotConnectRepository_ReturnsUnhealthyStatus()
    {
        // Arrange
        var repository = Substitute.For<ICosmosDbRepository>();
        repository
            .CanConnect()
            .Returns(false);
        var healthCheck = new CosmosDbHealthCheck(repository, Substitute.For<ILogger<CosmosDbHealthCheck>>());

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status
            .Should()
            .Be(HealthStatus.Unhealthy);
    }
}