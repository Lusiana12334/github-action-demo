using Azure.Messaging.ServiceBus;
using FluentValidation;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PEXC.Case.DataContracts;
using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;
using PEXC.Case.Functions.Validators;
using PEXC.Case.Services;
using PEXC.Case.Services.Infrastructure;
using PEXC.Case.Services.Workflow;

namespace PEXC.Case.Functions.SurveyHandlers;

public class UpdateSurveyHandler : SurveyBaseHandler
{
    private readonly IProfileMapper _profileMapper;
    private readonly IWorkflowSurveyService _surveyService;

    public UpdateSurveyHandler(
        IProfileMapper profileMapper,
        IWorkflowSurveyService surveyService,
        IOptions<CosmosChangeFeedOptions> options,
        ILogger<UpdateSurveyHandler> logger) : base(logger, options)
    {
        _profileMapper = profileMapper ?? throw new ArgumentNullException(nameof(profileMapper));
        _surveyService = surveyService ?? throw new ArgumentNullException(nameof(surveyService));
    }

    [FunctionName(nameof(UpdateSurveyHandler))]
    public Task Run(
        [ServiceBusTrigger(
            "%CosmosChangeFeedOptions:CaseChangeTopicName%",
            "%CosmosChangeFeedOptions:SurveyOpenedSubscription%",
            Connection = "ServiceBusOptions:ConnectionString")]
        ServiceBusReceivedMessage message) =>
        HandleMessage(message, _surveyService.UpdateSurvey);

    protected override AbstractValidator<AsbMessageDto> GetValidator(string correlationId) =>
        new UpdateSurveyValidator(_profileMapper, correlationId);

    protected override string GeneralErrorMessage => "An exception occurred while updating a survey";
    protected override UserInfo ServiceUserInfo => new(UserType.Service, nameof(UpdateSurveyHandler));

    protected override AsbMessageDto? DeserializeMessage(ServiceBusReceivedMessage message)
    {
        var asbMessage = Utils.TypeAwareDeserialize<AsbMessageDto>(message.Body);
        if (asbMessage?.Entity == null) 
            return asbMessage;

        var caseEntity = _surveyService
            .GetCase(asbMessage.Entity.Id, asbMessage.Entity.Key)
            .Result;

        if (caseEntity == null)
            throw new ValidationException(
                $"The message could not be deserialized because the CaseEntity was not found. Case Id: {asbMessage.Entity.Id}");

        return asbMessage with { Entity = caseEntity };
    }
}