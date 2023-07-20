using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using PEXC.Case.Domain;
using PEXC.Common.Logging.Extensions;
using Azure.Messaging.ServiceBus;
using PEXC.Case.DataContracts;
using PEXC.Case.DataContracts.V1;

namespace PEXC.Case.Functions;

public class CosmosChangeFeedHandlerFunction
{
    private readonly ILogger<CosmosChangeFeedHandlerFunction> _logger;

    public CosmosChangeFeedHandlerFunction(ILogger<CosmosChangeFeedHandlerFunction> logger)
        => _logger = logger;

    [FunctionName(nameof(CosmosChangeFeedHandlerFunction))]
    public void Run(
        [CosmosDBTrigger(
            databaseName: "%CosmosOptions:Database%",
            collectionName: "%CosmosOptions:Container%",
            ConnectionStringSetting = "CosmosOptions:ConnectionString",
            LeaseCollectionName = "%CosmosOptions:LeasesContainer%",
            CreateLeaseCollectionIfNotExists = true)] IReadOnlyList<dynamic> documents,
        [ServiceBus("%CosmosChangeFeedOptions:CaseChangeTopicName%",
            Connection = "ServiceBusOptions:ConnectionString")] ICollector<ServiceBusMessage> serviceBusOutput)
    {
        try
        {
            if (documents.Count <= 0)
            {
                _logger.LogInformation("Nothing has been changed");
                return;
            }

            _logger.LogInformation("Changes detected for items. Ids: {itemIds}",
                string.Join(", ", documents.Select(d => d.Id)));

            IEnumerable<IEntity> documentsToSend = documents
                .Select(document => document.type switch
                {
                    // code below is a way to deserialize dynamic representation of JSON to particular IEntity instance
                    // without information of the class type system is not able to serialize message
                    nameof(CaseEntity) => (IEntity)(CaseEntity)document,
                    nameof(CaseDataImportState) => (CaseDataImportState)document,
                    nameof(IrisDataImportState) => (IrisDataImportState)document,
                    _ => throw new NotSupportedException(document.type)
                })
                .ToList();

            foreach (var entity in documentsToSend)
            {
                SendMessage(entity, serviceBusOutput);
            }
        }
        catch (Exception ex)
        {
            _logger.LogCriticalWithTelemetry(ex, ex.Message);
            throw;
        }
    }

    private void SendMessage(IEntity entity, ICollector<ServiceBusMessage> serviceBusOutput)
    {
        using var _ = _logger.BeginScope(entity.CorrelationId.ToString(), entity.Key);

        try
        {
            _logger.LogInformation("Going to send a message");

            var message = CreateMessage(entity, entity.CorrelationId);
            serviceBusOutput.Add(message);

            _logger.LogInformation("Message was sent");
        }
        catch (Exception exception)
        {
            _logger.LogCriticalWithTelemetry(exception, "An exception occurred while sending a message");
        }
    }

    private static ServiceBusMessage CreateMessage(IEntity entity, Guid correlationId)
    {
        var messageDto = new AsbMessageDto(correlationId, entity);

        var serializedContent = Utils.TypeAwareSerialize(messageDto);

        var message = new ServiceBusMessage(serializedContent);

        AddMessageProperties(message.ApplicationProperties, entity, correlationId);

        return message;
    }

    private static void AddMessageProperties(
        IDictionary<string, object> properties, IEntity entity, Guid correlationId)
    {
        properties.Add(nameof(IEntity.Type), entity.Type);

        if (entity is CaseEntity caseEntity)
        {
            properties.Add(nameof(CaseEntity.EndDate), caseEntity.EndDate!);
            properties.Add(nameof(CaseEntity.ItemStage), caseEntity.ItemStage.ToString());
            properties.Add($"{nameof(CaseEntity.ModifiedBy)}_{nameof(CaseEntity.ModifiedBy.UserType)}", caseEntity.ModifiedBy?.UserType.ToString() ?? string.Empty);
        }

        properties.Add(LoggerConsts.CorrelationIdProperty, correlationId.ToString());
        properties.Add(LoggerConsts.CaseCodeProperty, entity.Key);
        properties.Add(LoggerConsts.IdProperty, entity.Id);
    }
}