using Azure.Messaging.ServiceBus;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PEXC.Case.DataContracts;
using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;
using PEXC.Case.Services.Infrastructure;
using PEXC.Common.Logging.Extensions;

namespace PEXC.Case.Functions.SurveyHandlers;

public abstract class SurveyBaseHandler
{
    private readonly ILogger _logger;
    private readonly CosmosChangeFeedOptions _options;

    protected SurveyBaseHandler(ILogger logger, IOptions<CosmosChangeFeedOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger)); 
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    protected abstract string GeneralErrorMessage { get; }
    protected abstract UserInfo ServiceUserInfo { get; }
    protected abstract AbstractValidator<AsbMessageDto> GetValidator(string correlationId);

    protected virtual AsbMessageDto? DeserializeMessage(ServiceBusReceivedMessage message) 
        => Utils.TypeAwareDeserialize<AsbMessageDto>(message.Body);

    protected async Task HandleMessage(ServiceBusReceivedMessage message, Func<CaseEntity, UserInfo, string, Task> handlerAction)
    {
        message.ApplicationProperties.TryGetValue(LoggerConsts.CorrelationIdProperty, out var correlationId);
        message.ApplicationProperties.TryGetValue(LoggerConsts.CaseCodeProperty, out var caseCode);
        message.ApplicationProperties.TryGetValue(LoggerConsts.IdProperty, out var id);

        using var _ = _logger.BeginScope(correlationId!.ToString(), caseCode?.ToString(), id?.ToString(),
            new Dictionary<string, object>()
            {
                { nameof(message.DeliveryCount), message.DeliveryCount },
                { "AlertRule", "CaseSurvey" }
            });

        try
        {
            var (isValid, asbMessage) = await ParseAndValidateMessage(message, correlationId.ToString()!);
            if (!isValid)
            {
                return;
            }

            var caseEntity = (CaseEntity)asbMessage!.Entity;
            await handlerAction(caseEntity, ServiceUserInfo, correlationId.ToString()!);
        }
        catch (Exception ex)
        {
            _logger.LogAsbFunctionFailError(ex,GeneralErrorMessage, message.DeliveryCount, _options.MaxDeliveryCount);
            throw;
        }
    }

    private async Task<(bool, AsbMessageDto? message)> ParseAndValidateMessage(ServiceBusReceivedMessage message, string correlationId)
    {
        AsbMessageDto? asbMessage;
        try
        {
            asbMessage = DeserializeMessage(message);
        }
        catch (Exception ex) when (ex is ValidationException or JsonException)
        {
            _logger.LogErrorWithTelemetry(ex, "Validation failed");
            return (false, null);
        }

        if (asbMessage == null)
        {
            _logger.LogErrorWithTelemetry(
                new ValidationException("Cannot deserialize message"),
                "Validation failed");

            return (false, null);
        }

        var validator = GetValidator(correlationId);
        var validationResult = await validator.ValidateAsync(asbMessage);

        if (validationResult.IsValid)
        {
            return (true, asbMessage);
        }

        var hasErrors = validationResult.Errors.Any(item => item.Severity == Severity.Error);
        if (hasErrors)
        {
            _logger.LogErrorWithTelemetry(
                new ValidationException(validationResult.Errors),
                "Validation failed");
            return (false, asbMessage);
        }

        var information = validationResult.Errors.Where(item => item.Severity != Severity.Error).ToList();
        if (information.Any())
        {
            _logger.LogInformation(
                "Validation information. Processing stopped without error [{reasons}]",
                string.Join(Environment.NewLine, information.Select(e => e.ErrorMessage)));
        }
        return (false, asbMessage);

    }
}