using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PEXC.Case.DataAccess.CosmosDB;
using PEXC.Case.DataAccess.CosmosDB.Infrastructure;
using PEXC.Case.DataContracts;
using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;
using PEXC.Case.Services.Infrastructure;
using PEXC.Common.Logging.Extensions;

namespace PEXC.Case.Functions;

public class AuditHandlerFunction
{
    private readonly CosmosChangeFeedOptions _cosmosChangeFeedOptions;
    private readonly ILogger<AuditHandlerFunction> _logger;
    private readonly ICosmosDbRepository _auditRepository;

    public AuditHandlerFunction(
        Func<string, ICosmosDbRepository> cosmosDbRepositoryFactory,
        IOptions<CosmosOptions> options,
        IOptions<CosmosChangeFeedOptions> cosmosChangeFeedOptions,
        ILogger<AuditHandlerFunction> log)
    {
        if (cosmosDbRepositoryFactory == null) throw new ArgumentNullException(nameof(cosmosDbRepositoryFactory));
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrEmpty(options.Value.AuditContainer))
        {
            throw new ArgumentException(nameof(CosmosOptions.AuditContainer));
        }

        _auditRepository = cosmosDbRepositoryFactory(options.Value.AuditContainer);
        _cosmosChangeFeedOptions = cosmosChangeFeedOptions?.Value ?? throw new ArgumentNullException(nameof(cosmosChangeFeedOptions));
        _logger = log;
    }

    [FunctionName(nameof(AuditHandlerFunction))]
    public async Task Run(
        [ServiceBusTrigger(
            "%CosmosChangeFeedOptions:CaseChangeTopicName%",
            "%CosmosChangeFeedOptions:AuditSubscription%",
            Connection = "ServiceBusOptions:ConnectionString")]
        ServiceBusReceivedMessage message)
    {
        message.ApplicationProperties.TryGetValue(LoggerConsts.CorrelationIdProperty, out var correlationId);
        message.ApplicationProperties.TryGetValue(LoggerConsts.CaseCodeProperty, out var caseCode);
        message.ApplicationProperties.TryGetValue(LoggerConsts.IdProperty, out var id);
        message.ApplicationProperties.TryGetValue($"{nameof(CaseEntity.ModifiedBy)}_{nameof(CaseEntity.ModifiedBy.UserType)}", out var modifiedBy);


        using var _ = _logger.BeginScope(correlationId!.ToString(), caseCode?.ToString(), id?.ToString(),
            new Dictionary<string, object>() { { nameof(modifiedBy), modifiedBy ?? string.Empty } });
        try
        {
            var messageDto = Utils.TypeAwareDeserialize<AsbMessageDto>(message.Body);

            if (messageDto?.Entity == null)
            {
                _logger.LogCritical("Entity cannot be deserialized! {entityString}", message.Body.ToString());
                return;
            }

            //We cannot do explicit call to _auditRepository.CreateDocument with IEntity as generic parameter -> method will fail after insert during deserialization
            _logger.LogInformation("Creating audit entity...");
            var entityAudit = CreateAudit(messageDto);
            if (entityAudit is null)
            {
                _logger.LogCritical("Audit entry has not been created !{ entityString}", message.Body.ToString());
                return;
            }
            _logger.LogInformation("Saving audit entity..");
            await AddAudit(entityAudit);

            _logger.LogInformation("Audit entity saved");
        }
        catch (Exception ex)
        {

            _logger.LogAsbFunctionFailError(ex, "An exception occurred while collecting audit data", message.DeliveryCount, _cosmosChangeFeedOptions.MaxDeliveryCount);
            throw;
        }
    }

    private async Task AddAudit(object entityAudit)
    {
        var createDocumentMethod = typeof(ICosmosDbRepository).GetMethod(nameof(ICosmosDbRepository.CreateDocument));
        var generic = createDocumentMethod!.MakeGenericMethod(entityAudit.GetType());
        var task = (Task)generic.Invoke(_auditRepository, new[] { entityAudit })!;
        await task.ConfigureAwait(false);
    }

    private static object? CreateAudit(AsbMessageDto messageDto)
    {
        Type[] typeArgs = { messageDto.Entity.GetType() };
        var auditEntryType = typeof(AuditEntry<>);
        var entityAuditType = auditEntryType.MakeGenericType(typeArgs);
        var entityAudit = Activator.CreateInstance(entityAuditType, messageDto.Entity);
        return entityAudit;
    }
}