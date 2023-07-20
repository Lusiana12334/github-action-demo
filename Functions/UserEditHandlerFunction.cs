using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PEXC.Case.DataContracts;
using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;
using PEXC.Case.Functions.SurveyHandlers;
using PEXC.Case.Services;
using PEXC.Case.Services.CCM;
using PEXC.Case.Services.Coveo;
using PEXC.Case.Services.Infrastructure;
using PEXC.Case.Services.IRIS;
using PEXC.Case.Services.Mapping;
using PEXC.Common.Logging.Extensions;

namespace PEXC.Case.Functions;

public class UserEditHandlerFunction
{
    private static long _lockDebounceCount;
    private readonly ICoveoRefreshService _coveoRefreshService;
    private readonly CosmosChangeFeedOptions _cosmosChangeFeedOptions;
    private readonly CoveoApiOptions _coveoApiOptions;
    private readonly ILogger<UserEditHandlerFunction> _logger;

    public UserEditHandlerFunction(ICoveoRefreshService coveoRefreshService,
        IOptions<CoveoApiOptions> options,
        IOptions<CosmosChangeFeedOptions> cosmosChangeFeedOptions,
        ILogger<UserEditHandlerFunction> log)
    {
        _coveoRefreshService = coveoRefreshService ?? throw new ArgumentNullException(nameof(coveoRefreshService));
        _logger = log ?? throw new ArgumentNullException(nameof(log));

        _cosmosChangeFeedOptions = cosmosChangeFeedOptions?.Value ?? throw new ArgumentNullException(nameof(cosmosChangeFeedOptions));
        _coveoApiOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    [FunctionName(nameof(RefreshSearchIndex))]
    public async Task RefreshSearchIndex(
        [ServiceBusTrigger(
            "%CosmosChangeFeedOptions:CaseChangeTopicName%",
            "%CosmosChangeFeedOptions:UserEditSubscription%",
            Connection = "ServiceBusOptions:ConnectionString")]
        ServiceBusReceivedMessage message)
    {
        message.ApplicationProperties.TryGetValue(LoggerConsts.CorrelationIdProperty, out var correlationId);
        message.ApplicationProperties.TryGetValue(LoggerConsts.CaseCodeProperty, out var caseCode);
        message.ApplicationProperties.TryGetValue(LoggerConsts.IdProperty, out var id);
        message.ApplicationProperties.TryGetValue(nameof(IEntity.Type), out var type);

        using var _ = _logger.BeginScope(correlationId!.ToString(), caseCode?.ToString(), id?.ToString(),
            new Dictionary<string, object?>() { { nameof(IEntity.Type), type } });

        try
        {
            var scheduleRefresh = ShouldScheduleRefresh(message, type);

            if (scheduleRefresh)
            {
                await Debounce(_coveoRefreshService.RefreshCaseManagementIndex,
                    _coveoApiOptions.MinSearchRefreshDebounceInSeconds);
            }
            else
            {
                _logger.LogInformation("The message does not trigger refresh");
            }
        }
        catch (Exception ex)
        {
            _logger.LogAsbFunctionFailError(ex,
                "An exception occurred while refreshing the search index.",
                message.DeliveryCount,
                _cosmosChangeFeedOptions.MaxDeliveryCount);
            throw;
        }
    }

    private static bool ShouldScheduleRefresh(ServiceBusReceivedMessage message, object? type)
    {
        if (type is not string entityType)
        {
            return false;
        }

        return entityType switch
        {
            nameof(CaseDataImportState) => true,
            nameof(IrisDataImportState) => true,
            nameof(CaseEntity) => ShouldSchedulerRefreshForCaseEntity(message),
            _ => false
        };
    }

    private static bool ShouldSchedulerRefreshForCaseEntity(ServiceBusReceivedMessage message)
    {
        var asbMessage = Utils.TypeAwareDeserialize<AsbMessageDto>(message.Body);

        if (asbMessage?.Entity is not CaseEntity caseEntity || caseEntity.ModifiedBy == null)
        {
            return false;
        }

        return caseEntity.ModifiedBy!.DisplayName switch
        {
            nameof(EndSurveyHandler) => true,
            nameof(StartSurveyHandler) => true,
            nameof(UpdateSurveyHandler) => true,

            nameof(IrisDataImportService) => false,
            nameof(CaseDataImportService) => false,
            CaseSearchabilityService.SearchableCasesCrawlerServiceUserName => false,
            MainProfile.MigrationUserDisplayName => false,

            _ => true // manual case change
        };
    }

    private async Task Debounce(Func<Task> action, int seconds)
    {
        var newLockCount = Interlocked.Increment(ref _lockDebounceCount);
        if (newLockCount > 1)
        {
            _logger.LogInformation("Action has been already scheduled, skipping this invocation");
            return;
        }

        try
        {
            _logger.LogInformation("Action will be scheduled after {refresh_seconds} seconds", seconds);
            await Task.Delay(TimeSpan.FromSeconds(seconds));
        }
        finally
        {
            Interlocked.Exchange(ref _lockDebounceCount, 0);
        }

        _logger.LogInformation("Executing action..");
        await action();
        _logger.LogInformation("Action executed");
    }
}