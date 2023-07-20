using Azure.Messaging.ServiceBus;
using FluentValidation;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;
using PEXC.Case.Functions.Validators;
using PEXC.Case.Services.Infrastructure;
using PEXC.Case.Services.Workflow;

namespace PEXC.Case.Functions.SurveyHandlers;

public class ScheduleSurveyHandler : SurveyBaseHandler
{
    private readonly IWorkflowSurveyService _surveyService;

    public ScheduleSurveyHandler(
        IWorkflowSurveyService surveyService, 
        ILogger<ScheduleSurveyHandler> logger, 
        IOptions<CosmosChangeFeedOptions> options) : base(logger, options)
    {
        _surveyService = surveyService;
    }


    [FunctionName(nameof(ScheduleSurveyHandler))]
    public Task Run(
        [ServiceBusTrigger(
            "%CosmosChangeFeedOptions:CaseChangeTopicName%",
            "%CosmosChangeFeedOptions:ScheduleSurveySubscription%",
            Connection = "ServiceBusOptions:ConnectionString")]
        ServiceBusReceivedMessage message) =>
        HandleMessage(message, _surveyService.ScheduleSurvey);

    protected override AbstractValidator<AsbMessageDto> GetValidator(string correlationId) => new ScheduleSurveyValidator();

    protected override string GeneralErrorMessage => "An exception occurred while scheduling a survey";
    protected override UserInfo ServiceUserInfo => new(UserType.Service, nameof(ScheduleSurveyHandler));
}