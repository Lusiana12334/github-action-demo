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

public class EndSurveyHandler : SurveyBaseHandler
{
    private readonly IWorkflowSurveyService _surveyService;
    private readonly IProfileMapper _profileMapper;

    public EndSurveyHandler(
        IWorkflowSurveyService surveyService,
        IProfileMapper profileMapper,
        IOptions<CosmosChangeFeedOptions> options,
        ILogger<EndSurveyHandler> logger) : base(logger, options)
    {
        _surveyService = surveyService ?? throw new ArgumentNullException(nameof(surveyService));
        _profileMapper = profileMapper ?? throw new ArgumentNullException(nameof(profileMapper));
    }

    [FunctionName(nameof(EndSurveyHandler))]
    public Task Run(
        [ServiceBusTrigger(
            "%CosmosChangeFeedOptions:CaseChangeTopicName%",
            "%CosmosChangeFeedOptions:EndSurveySubscription%",
            Connection = "ServiceBusOptions:ConnectionString")]
        ServiceBusReceivedMessage message) =>
        HandleMessage(message, _surveyService.CloseSurvey);

    protected override AbstractValidator<AsbMessageDto> GetValidator(string correlationId) =>
        new EndSurveyValidator(_profileMapper, correlationId);

    protected override string GeneralErrorMessage => "An exception occurred while closing a survey";
    protected override UserInfo ServiceUserInfo => new(UserType.Service, nameof(EndSurveyHandler));
}