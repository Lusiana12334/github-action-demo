using Azure.Messaging.ServiceBus;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PEXC.Case.DataAccess;
using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;
using PEXC.Case.Functions.Validators;
using PEXC.Case.Services.Infrastructure;
using PEXC.Case.Services.Workflow;
using PEXC.Common.ServiceBus.Contracts;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace PEXC.Case.Functions.SurveyHandlers;

public class TriggerSurveyHandler : SurveyBaseHandler
{
    private readonly IWorkflowSurveyService _surveyService;
    private readonly ISingleCaseRepository _singleCaseRepository;
    private readonly IEventDistributionService _eventDistributionService;
    private readonly WorkflowSurveyOptions _workflowOptions;

    public TriggerSurveyHandler(
        IWorkflowSurveyService surveyService,
        ISingleCaseRepository singleCaseRepository,
        IEventDistributionService eventDistributionService,
        IOptions<WorkflowSurveyOptions> workflowOptions,
        ILogger<TriggerSurveyHandler> logger,
        IOptions<CosmosChangeFeedOptions> options) : base(logger, options)
    {
        _surveyService = surveyService;
        _singleCaseRepository = singleCaseRepository;
        _eventDistributionService = eventDistributionService;
        _workflowOptions = workflowOptions.Value;
    }
    
    [FunctionName(nameof(CancelTriggerSurveyMessage))]
    public async Task<IActionResult> CancelTriggerSurveyMessage(
#if DEBUG
        [HttpTrigger("get", Route = null)]
#else
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
#endif
        HttpRequest req)
    {
        if (!long.TryParse(req.Query["sequenceNumber"], out var sequenceNumber))
        {
            return new BadRequestResult();
        }

        await _eventDistributionService.CancelEvent(_workflowOptions.TriggerSurveyQueue, sequenceNumber);
        return new OkObjectResult("OK");

    }


    [FunctionName(nameof(TriggerSurveyHandler))]
    public Task Run(
        [ServiceBusTrigger(
            "%WorkflowSurveyOptions:TriggerSurveyQueue%",
            Connection = "ServiceBusOptions:ConnectionString")]
        ServiceBusReceivedMessage message) =>
        HandleMessage(message, _surveyService.TriggerSurvey);

    protected override AbstractValidator<AsbMessageDto> GetValidator(string correlationId) => new TriggerSurveyValidator(_singleCaseRepository);

    protected override string GeneralErrorMessage => "An exception occurred while triggering a survey";
    protected override UserInfo ServiceUserInfo => new(UserType.Service, nameof(TriggerSurveyHandler));
}