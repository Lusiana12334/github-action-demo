using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PEXC.Case.Tests.Common;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json.Serialization;
using PEXC.Case.Domain;
using PEXC.Common.Logging.Extensions;

namespace PEXC.Case.Functions.Tests;

public class CosmosChangeFeedHandlerFunctionTests
{
    private static readonly JsonSerializerSettings SerializerSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
    };

    [Fact]
    public void UpsertDocument()
    {
        // Arrange
        var entity1 = Fake.CaseEntity("1");
        var entity2 = Fake.CaseEntity("2");
        var documentList = new List<Microsoft.Azure.Documents.Document>
        {
            ConvertToDocument(entity1),
            ConvertToDocument(entity2)
        };

        var logger = Substitute.For<ILogger<CosmosChangeFeedHandlerFunction>>();
        var collector = Substitute.For<ICollector<ServiceBusMessage>>();
        collector.Add(Arg.Any<ServiceBusMessage>());

        var changeFeedFunction = new CosmosChangeFeedHandlerFunction(logger);

        // Act
        changeFeedFunction.Run(documentList, collector);

        // Assert
        const string UserTypeKey = $"{nameof(CaseEntity.ModifiedBy)}_{nameof(CaseEntity.ModifiedBy.UserType)}";
        collector.Received(2).Add(Arg.Is<ServiceBusMessage>(m =>
            m.ApplicationProperties.ContainsKey(nameof(CaseEntity.ItemStage)) &&
            m.ApplicationProperties.ContainsKey(LoggerConsts.CaseCodeProperty) &&
            m.ApplicationProperties.ContainsKey(nameof(CaseEntity.EndDate)) &&
            m.ApplicationProperties.ContainsKey(UserTypeKey) &&
            m.ApplicationProperties[UserTypeKey].ToString() == UserType.User.ToString() &&
            m.ApplicationProperties.ContainsKey(LoggerConsts.CorrelationIdProperty) &&
            (Guid.Parse(m.ApplicationProperties[LoggerConsts.CorrelationIdProperty].ToString()!) == entity1.CorrelationId ||
             Guid.Parse(m.ApplicationProperties[LoggerConsts.CorrelationIdProperty].ToString()!) == entity2.CorrelationId)));
    }

    private static Microsoft.Azure.Documents.Document ConvertToDocument(object item)
    {
        var document = new Microsoft.Azure.Documents.Document();
        var serializedItem = JsonConvert.SerializeObject(item, SerializerSettings);
        document.LoadFrom(new JsonTextReader(new StringReader(serializedItem)));
        return document;
    }
}