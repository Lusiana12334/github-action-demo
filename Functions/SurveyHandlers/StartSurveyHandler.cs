using Azure.Messaging.ServiceBus;
using FluentValidation;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;
using PEXC.Case.Functions.Validators;
using PEXC.Case.Services;
using PEXC.Case.Services.Infrastructure;
using PEXC.Case.Services.Workflow;

namespace PEXC.Case.Functions.SurveyHandlers;

public class StartSurveyHandler : SurveyBaseHandler
{
    private readonly IProfileMapper _profileMapper;
    private readonly IWorkflowSurveyService _surveyService;

    public StartSurveyHandler(
        IProfileMapper profileMapper,
        IWorkflowSurveyService surveyService,
        IOptions<CosmosChangeFeedOptions> options,
        ILogger<StartSurveyHandler> logger) : base(logger, options)
    {
        _profileMapper = profileMapper ?? throw new ArgumentNullException(nameof(profileMapper));
        _surveyService = surveyService ?? throw new ArgumentNullException(nameof(surveyService));
    }

    [FunctionName(nameof(StartSurveyHandler))]
    public Task Run(
        [ServiceBusTrigger(
            "%CosmosChangeFeedOptions:CaseChangeTopicName%",
            "%CosmosChangeFeedOptions:StartSurveySubscription%",
            Connection = "ServiceBusOptions:ConnectionString")]
        ServiceBusReceivedMessage message) =>
        HandleMessage(message, _surveyService.StartSurvey);

    protected override AbstractValidator<AsbMessageDto> GetValidator(string correlationId) =>
        new StartSurveyValidator(_profileMapper, correlationId);

    protected override string GeneralErrorMessage => "An exception occurred while starting a survey";
    protected override UserInfo ServiceUserInfo => new(UserType.Service, nameof(StartSurveyHandler));
}