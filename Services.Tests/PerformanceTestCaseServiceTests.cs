using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PEXC.Case.DataAccess;
using PEXC.Case.Domain;
using PEXC.Case.Services.Infrastructure;

namespace PEXC.Case.Services.Tests;

public class PerformanceTestCaseServiceTests
{
    [Fact]
    public async Task DeleteCases_TwoPerfTestCases_ReturnsAndDeletesBothCases()
    {
        // Arrange
        var repository = Substitute.For<IPerformanceTestCaseRepository>();
        var performanceTestCase1 = new CaseEntity("1", "PERF_11111", RelationshipType.NonRetainer)
        { CorrelationId = Guid.NewGuid() };
        var performanceTestCase2 = new CaseEntity("2", "PERF_22222", RelationshipType.Retainer)
        { CorrelationId = Guid.NewGuid() };
        var correlationId = Guid.Parse("f6ec2a8b-0f78-4315-ac51-2e25f8bbb6e3");
       
        repository.DeleteCaseDocument("1", "PERF_11111", correlationId).Returns(Task.FromResult(true));
        repository.DeleteCaseDocument("2", "PERF_22222", correlationId).Returns(Task.FromResult(true));
        repository.DeleteCaseDocument("3", "NP11111", correlationId).Returns(Task.FromException<bool>(new Exception()));
        repository.DeleteCaseDocument("4", "NP22222", correlationId).Returns(Task.FromException<bool>(new Exception()));

        var pagedResult = new PagedResult<CaseEntity>() { Items = new[] { performanceTestCase1, performanceTestCase2 } };

        repository.GetCasesCreatedByPerformanceTests(5).Returns(Task.FromResult(pagedResult));

        var logger = Substitute.For<ILogger<PerformanceTestCaseService>>();
        var service = CreateService(logger, repository);

        // Act
        await service.DeleteCases(correlationId, databaseQueryPageSize: 5);

        // Assert
        await repository.Received(2).DeleteCaseDocument(Arg.Any<string>(), Arg.Any<string>(), Guid.Parse("f6ec2a8b-0f78-4315-ac51-2e25f8bbb6e3"));
        await repository.Received(1).DeleteCaseDocument("1", "PERF_11111", Guid.Parse("f6ec2a8b-0f78-4315-ac51-2e25f8bbb6e3"));
        await repository.Received(1).DeleteCaseDocument("2", "PERF_22222", Guid.Parse("f6ec2a8b-0f78-4315-ac51-2e25f8bbb6e3"));
        await repository.Received(1).GetCasesCreatedByPerformanceTests(5);
    }

    [Fact]
    public async Task DeleteCases_FourPerfTestCasesTwoPages_ReturnsAndDeletesFourCases()
    {
        // Arrange
        var repository = Substitute.For<IPerformanceTestCaseRepository>();
        var performanceTestCase1 = new CaseEntity("1", "PERF_11111", RelationshipType.NonRetainer)
        { CorrelationId = Guid.NewGuid() };
        var performanceTestCase2 = new CaseEntity("2", "PERF_22222", RelationshipType.Retainer)
        { CorrelationId = Guid.NewGuid() };
        var performanceTestCase3 = new CaseEntity("5", "PERF_33333", RelationshipType.NonRetainer)
        { CorrelationId = Guid.NewGuid() };
        var performanceTestCase4 = new CaseEntity("6", "PERF_44444", RelationshipType.Retainer)
        { CorrelationId = Guid.NewGuid() };
        var correlationId = Guid.Parse("f6ec2a8b-0f78-4315-ac51-2e25f8bbb6e3");

        repository.DeleteCaseDocument("1", "PERF_11111", correlationId).Returns(Task.FromResult(true));
        repository.DeleteCaseDocument("2", "PERF_22222", correlationId).Returns(Task.FromResult(true));
        repository.DeleteCaseDocument("5", "PERF_33333", correlationId).Returns(Task.FromResult(true));
        repository.DeleteCaseDocument("6", "PERF_44444", correlationId).Returns(Task.FromResult(true));

        var pagedResult1 = new PagedResult<CaseEntity>() { Items = new[] { performanceTestCase1, performanceTestCase2, performanceTestCase3 } };
        var pagedResult2 = new PagedResult<CaseEntity>() { Items = new[] { performanceTestCase4 } };

        repository.GetCasesCreatedByPerformanceTests(3).Returns(Task.FromResult(pagedResult1), Task.FromResult(pagedResult2));

        var logger = Substitute.For<ILogger<PerformanceTestCaseService>>();
        var service = CreateService(logger, repository);

        // Act
        await service.DeleteCases(correlationId, databaseQueryPageSize: 3);

        // Assert
        await repository.Received(4).DeleteCaseDocument(Arg.Any<string>(), Arg.Any<string>(), Guid.Parse("f6ec2a8b-0f78-4315-ac51-2e25f8bbb6e3"));
        await repository.Received(1).DeleteCaseDocument("1", "PERF_11111", Guid.Parse("f6ec2a8b-0f78-4315-ac51-2e25f8bbb6e3"));
        await repository.Received(1).DeleteCaseDocument("2", "PERF_22222", Guid.Parse("f6ec2a8b-0f78-4315-ac51-2e25f8bbb6e3"));
        await repository.Received(1).DeleteCaseDocument("5", "PERF_33333", Guid.Parse("f6ec2a8b-0f78-4315-ac51-2e25f8bbb6e3"));
        await repository.Received(1).DeleteCaseDocument("6", "PERF_44444", Guid.Parse("f6ec2a8b-0f78-4315-ac51-2e25f8bbb6e3"));
        await repository.Received(2).GetCasesCreatedByPerformanceTests(3);
    }

    [Fact]
    public async Task DeleteCases_FourPerfTestCasesTwoPages_ThirdFails_DeletesTwoCasesAndFails()
    {
        // Arrange
        var repository = Substitute.For<IPerformanceTestCaseRepository>();
        var performanceTestCase1 = new CaseEntity("1", "PERF_11111", RelationshipType.NonRetainer)
        { CorrelationId = Guid.NewGuid() };
        var performanceTestCase2 = new CaseEntity("2", "PERF_22222", RelationshipType.Retainer)
        { CorrelationId = Guid.NewGuid() };
        var performanceTestCase3 = new CaseEntity("5", "PERF_33333", RelationshipType.NonRetainer)
        { CorrelationId = Guid.NewGuid() };
        var performanceTestCase4 = new CaseEntity("6", "PERF_44444", RelationshipType.Retainer)
        { CorrelationId = Guid.NewGuid() };
        var correlationId = Guid.Parse("f6ec2a8b-0f78-4315-ac51-2e25f8bbb6e3");

        repository.DeleteCaseDocument("1", "PERF_11111", correlationId).Returns(Task.FromResult(true));
        repository.DeleteCaseDocument("2", "PERF_22222", correlationId).Returns(Task.FromResult(true));
        repository.DeleteCaseDocument("5", "PERF_33333", correlationId).Returns(Task.FromException<bool>(new Exception("Sorry not found.")));
        repository.DeleteCaseDocument("6", "PERF_44444", correlationId).Returns(Task.FromResult(true));

        var pagedResult1 = new PagedResult<CaseEntity>() { Items = new[] { performanceTestCase1, performanceTestCase2, performanceTestCase3 } };
        var pagedResult2 = new PagedResult<CaseEntity>() { Items = new[] { performanceTestCase4 } };

        repository.GetCasesCreatedByPerformanceTests(3).Returns(Task.FromResult(pagedResult1), Task.FromResult(pagedResult2));

        var logger = Substitute.For<ILogger<PerformanceTestCaseService>>();
        var service = CreateService(logger, repository);

        // Act
        await service.DeleteCases(correlationId, databaseQueryPageSize: 3);

        // Assert
        await repository.Received(3).DeleteCaseDocument(Arg.Any<string>(), Arg.Any<string>(), Guid.Parse("f6ec2a8b-0f78-4315-ac51-2e25f8bbb6e3"));
        await repository.Received(1).DeleteCaseDocument("1", "PERF_11111", Guid.Parse("f6ec2a8b-0f78-4315-ac51-2e25f8bbb6e3"));
        await repository.Received(1).DeleteCaseDocument("2", "PERF_22222", Guid.Parse("f6ec2a8b-0f78-4315-ac51-2e25f8bbb6e3"));
        await repository.Received(1).DeleteCaseDocument("5", "PERF_33333", Guid.Parse("f6ec2a8b-0f78-4315-ac51-2e25f8bbb6e3"));
        await repository.Received(1).GetCasesCreatedByPerformanceTests(3);
        AssertLog(logger, LogLevel.Error, expectedTimesReceived: 1, expectedMessage: "Failed to delete case. Id: 5 Key: PERF_33333");
        AssertLog(logger, LogLevel.Error, expectedTimesReceived: 1, expectedMessage: "Stopping performance test cases deletion.");
    }

    private static void AssertLog(ILogger<PerformanceTestCaseService> logger, LogLevel logLevel, int expectedTimesReceived, string expectedMessage)
    {
        var timesReceived = logger.ReceivedCalls()
        .Select(call => call.GetArguments())
        .Count(callArguments => (callArguments[0] as LogLevel?).Equals(logLevel) &&
                                (callArguments[2] as IReadOnlyList<KeyValuePair<string, object>>)!.Last().Value.ToString()!.Equals(expectedMessage));
        Assert.True(timesReceived == expectedTimesReceived, 
            $"Expected {expectedTimesReceived} log {logLevel} calls with message \"{expectedMessage}\" but found {timesReceived}");
    }
    
    private PerformanceTestCaseService CreateService(ILogger<PerformanceTestCaseService> logger, 
                                                     IPerformanceTestCaseRepository respository)
    {
        
        return new PerformanceTestCaseService(logger, respository);
    }

}