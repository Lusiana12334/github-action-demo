using Azure.Messaging.ServiceBus;
using PEXC.Case.DataContracts;
using PEXC.Case.DataContracts.V1;
using PEXC.Common.Logging.Extensions;

namespace PEXC.Case.Functions.Tests;

public static class TestsUtils
{
    public static ServiceBusReceivedMessage GetServiceBusMessage(AsbMessageDto asbMessage)
    {
        var messageAsJason = Utils.TypeAwareSerialize(asbMessage);
        var properties = new Dictionary<string, object>
        {
            { LoggerConsts.CorrelationIdProperty, asbMessage.CorrelationId },
            { LoggerConsts.CaseCodeProperty, asbMessage.Entity.Key }
        };
        return ServiceBusModelFactory
            .ServiceBusReceivedMessage(BinaryData.FromString(messageAsJason), properties: properties);
    }
}