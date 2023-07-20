using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PEXC.Case.DataAccess.CosmosDB;
using PEXC.Case.DataAccess.CosmosDB.Infrastructure;
using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;
using PEXC.Case.Services.Infrastructure;
using PEXC.Case.Tests.Common;

namespace PEXC.Case.Functions.Tests;

public class AuditHandlerFunctionTests
{
    [Fact]
    public async Task HandleMessage_CaseEntitySaved()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();

        var message = new AsbMessageDto(Guid.NewGuid(), caseEntity);

        var auditRepository = Substitute.For<ICosmosDbRepository>();

        var handler = CreateAuditHandler(auditRepository);

        var receivedMessage = TestsUtils.GetServiceBusMessage(message);

        // Act
        var result = handler.Run(receivedMessage);
        await result;

        // Assert
        result.IsCompletedSuccessfully.Should().BeTrue();

        await auditRepository
            .Received()
            .CreateDocument(Arg.Is<AuditEntry<CaseEntity>>(item =>
                item.Key == caseEntity.Id
                && item.Type == caseEntity.Type
                && item.AuditEntity.Id == caseEntity.Id));
    }

    [Fact]
    public async Task HandleMessage_CaseDataImportStateSaved()
    {
        // Arrange
        var caseDataImportState = new CaseDataImportState();

        var message = new AsbMessageDto(Guid.NewGuid(), caseDataImportState);

        var auditRepository = Substitute.For<ICosmosDbRepository>();

        var handler = CreateAuditHandler(auditRepository);

        var receivedMessage = TestsUtils.GetServiceBusMessage(message);

        // Act
        var result = handler.Run(receivedMessage);
        await result;

        // Assert
        result.IsCompletedSuccessfully.Should().BeTrue();

        await auditRepository
            .Received()
            .CreateDocument(Arg.Is<AuditEntry<CaseDataImportState>>(item =>
                item.Key == caseDataImportState.Id 
                && item.Type == caseDataImportState.Type
                && item.AuditEntity.Id == caseDataImportState.Id));
    }

    [Fact]
    public async Task HandleMessage_IrisDataImportStateSaved()
    {
        // Arrange
        var irisDataImportState = new IrisDataImportState();

        var message = new AsbMessageDto(Guid.NewGuid(), irisDataImportState);

        var auditRepository = Substitute.For<ICosmosDbRepository>();

        var handler = CreateAuditHandler(auditRepository);

        var receivedMessage = TestsUtils.GetServiceBusMessage(message);

        // Act
        var result = handler.Run(receivedMessage);
        await result;

        // Assert
        result.IsCompletedSuccessfully.Should().BeTrue();

        await auditRepository
            .Received()
            .CreateDocument(Arg.Is<AuditEntry<IrisDataImportState>>(item =>
                item.Key == irisDataImportState.Id
                && item.Type == irisDataImportState.Type
                && item.AuditEntity.Id == irisDataImportState.Id));
    }

    private static AuditHandlerFunction CreateAuditHandler(ICosmosDbRepository auditRepository)
    {
        var logger = Substitute.For<ILogger<AuditHandlerFunction>>();
        var cosmosOptions =
            Options.Create(new CosmosOptions() { AuditContainer = nameof(CosmosOptions.AuditContainer) });
        var cosmosChangeFeedOptions =
            Options.Create(new CosmosChangeFeedOptions() { MaxDeliveryCount = 10 });

        var factory = (string container) => container switch
        {
            nameof(CosmosOptions.AuditContainer) => auditRepository,
            _ => throw new InvalidOperationException(container)
        };

        var handler = new AuditHandlerFunction(factory, cosmosOptions, cosmosChangeFeedOptions, logger);
        return handler;
    }
}