using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;
using PEXC.Case.Functions.SurveyHandlers;
using PEXC.Case.Services;
using PEXC.Case.Services.Infrastructure;
using PEXC.Case.Services.Workflow;
using PEXC.Case.Tests.Common;
using PEXC.Common.BaseApi.Profile.DataContracts.V1;

namespace PEXC.Case.Functions.Tests.SurveyHandlers;

public class StartSurveyHandlerTests
{
    [Theory]
    [InlineData(null, "eCode"),
    InlineData("eCode", null),
    InlineData("eCode", ""),
    InlineData("", "eCode"),
    InlineData(null, null)]
    public async Task HandleMessage_ValidationFailed_DirectoryNotCreatedAndSurveyNotStarted(string managerEcode, string billingPartnerEcode)
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        caseEntity.BillingPartnerEcode = billingPartnerEcode;
        caseEntity.ManagerEcode = managerEcode;

        var message = new AsbMessageDto(Guid.NewGuid(), caseEntity);
        var surveyService = GetSurveyService();

        var employeeProfiles = new Dictionary<string, EmployeeDetailsDto>();
        var profileMapper = GetProfileMapper_ReturningEmployeeProfiles(employeeProfiles, message.CorrelationId.ToString());

        var startSurveyHandler = PrepareHandler(profileMapper, surveyService);
        var receivedMessage = TestsUtils.GetServiceBusMessage(message);

        // Act
        var result = startSurveyHandler.Run(receivedMessage);
        await result;

        // Assert
        result.IsCompletedSuccessfully.Should().BeTrue();

        await surveyService
            .DidNotReceive()
            .StartSurvey(Arg.Any<CaseEntity>(), Arg.Any<UserInfo>(), Arg.Any<string>());
    }

    [Fact]
    public async Task HandleMessage_ValidationFailed_EmployeeProfileNotFound()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();

        var message = new AsbMessageDto(Guid.NewGuid(), caseEntity);
        var surveyService = GetSurveyService();
        var employeeProfiles = new Dictionary<string, EmployeeDetailsDto>();
        var profileMapper = GetProfileMapper_ReturningEmployeeProfiles(employeeProfiles, message.CorrelationId.ToString());

        var startSurveyHandler = PrepareHandler(profileMapper, surveyService);
        var receivedMessage = TestsUtils.GetServiceBusMessage(message);

        // Act
        var result = startSurveyHandler.Run(receivedMessage);
        await result;

        // Assert
        result.IsCompletedSuccessfully.Should().BeTrue();

        await profileMapper
            .Received()
            .GetEmployeeProfiles(Arg.Any<IReadOnlyList<string>>(), message.CorrelationId.ToString());

        await surveyService
            .DidNotReceive()
            .StartSurvey(Arg.Any<CaseEntity>(), Arg.Any<UserInfo>(), Arg.Any<string>());
    }

    [Fact]
    public async Task HandleMessage_SurveyStarted()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();

        var message = new AsbMessageDto(Guid.NewGuid(), caseEntity);
        var surveyService = GetSurveyService();
        var employeeProfiles = new Dictionary<string, EmployeeDetailsDto>()
        {
            { caseEntity.ManagerEcode!, Fake.EmployeeDetails(caseEntity.ManagerEcode!) },
            { caseEntity.BillingPartnerEcode!, Fake.EmployeeDetails(caseEntity.BillingPartnerEcode!) }
        };

        var profileMapper = GetProfileMapper_ReturningEmployeeProfiles(employeeProfiles, message.CorrelationId.ToString());

        var startSurveyHandler = PrepareHandler(profileMapper, surveyService);
        var receivedMessage = TestsUtils.GetServiceBusMessage(message);

        // Act
        var result = startSurveyHandler.Run(receivedMessage);
        await result;

        // Assert
        result.IsCompletedSuccessfully.Should().BeTrue();

        await profileMapper
            .Received()
            .GetEmployeeProfiles(Arg.Any<IReadOnlyList<string>>(), message.CorrelationId.ToString());

        await surveyService
            .Received()
            .StartSurvey(Arg.Any<CaseEntity>(), Arg.Any<UserInfo>(), Arg.Any<string>());
    }

    private static IWorkflowSurveyService GetSurveyService()
    {
        var surveyService = Substitute.For<IWorkflowSurveyService>();
        return surveyService;
    }

    private static IProfileMapper GetProfileMapper_ReturningEmployeeProfiles(Dictionary<string, EmployeeDetailsDto> employeeProfiles, string correlationId)
    {
        var profileMapper = Substitute.For<IProfileMapper>();
        profileMapper
            .GetEmployeeProfiles(Arg.Any<IReadOnlyList<string>>(), correlationId)
            .Returns(employeeProfiles);

        return profileMapper;
    }



    private static StartSurveyHandler PrepareHandler(
        IProfileMapper profileMapper,
        IWorkflowSurveyService surveyService)
    {
        var logger = Substitute.For<ILogger<StartSurveyHandler>>();
        var options = Options.Create(new CosmosChangeFeedOptions());
        return new StartSurveyHandler(profileMapper, surveyService, options, logger);
    }
}