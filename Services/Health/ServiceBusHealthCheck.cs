using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PEXC.Case.Services.Infrastructure;
using PEXC.Common.Logging.Extensions;

namespace PEXC.Case.Services.Health;

public class ServiceBusHealthCheck : PexcHealthCheck
{
    public const string HealthCheckName = "Service Bus";

    private readonly ServiceBusClient _serviceBusClient;
    private readonly string _topicName;
    private readonly IReadOnlyCollection<string> _subscriptions;

    public ServiceBusHealthCheck(
        ServiceBusClient serviceBusClient,
        IOptions<CosmosChangeFeedOptions> changeFeedOptions,
        ILogger<ServiceBusHealthCheck> logger)
        : base(logger)
    {
        _serviceBusClient = serviceBusClient;
        var options = changeFeedOptions.Value;
        _topicName = options.CaseChangeTopicName;
        _subscriptions = new List<string>
        {
            options.AuditSubscription,
            options.UserEditSubscription,
            options.SurveyOpenedSubscription,
            options.StartSurveySubscription,
            options.EndSurveySubscription
        };
    }

    public override string Name => HealthCheckName;

    protected override async Task<HealthCheckResult> GetHealthStatus(HealthCheckContext context, CancellationToken cancellationToken = new())
    {
        foreach (var subscription in _subscriptions)
        {
            var peekMessageResult = await TryPeekMessage(_topicName, subscription, cancellationToken);
            if (peekMessageResult.Status != HealthStatus.Healthy)
                return peekMessageResult;
        }

        return HealthCheckResult.Healthy();
    }

    private async Task<HealthCheckResult> TryPeekMessage(
        string topicName,
        string subscriptionName,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var receiver = _serviceBusClient.CreateReceiver(topicName, subscriptionName);
            await receiver.PeekMessageAsync(cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            var errorMessage = $"Could not peek a message from '{topicName}/{subscriptionName}' Service Bus Subscription.";
            Logger.LogErrorWithTelemetry(ex, errorMessage);
            return HealthCheckResult.Unhealthy(errorMessage, ex);
        }
    }
}