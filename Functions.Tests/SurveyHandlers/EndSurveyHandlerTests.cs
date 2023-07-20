using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;
using PEXC.Case.Functions.SurveyHandlers;
using PEXC.Case.Services;
using PEXC.Case.Services.Infrastructure;
using PEXC.Case.Services.Workflow;
using PEXC.Case.Tests.Common;

namespace PEXC.Case.Functions.Tests.SurveyHandlers;

public class EndSurveyHandlerTests
{
    [Fact]
    public async Task HandleMessage_SurveyClosed()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        caseEntity.ItemStage = CaseState.SurveyClosing;
        caseEntity.SharePointDirectory = new SharePointDirectoryEntity
        {
            DirectoryId = "dirId",
            DriveId = "driId"
        };

        var message = new AsbMessageDto(Guid.NewGuid(), caseEntity);
        var profileMapper = GetProfileMapper_ReturningEmployeeProfile(new[] { caseEntity.ManagerEcode! });

        var surveyService = GetSurveyService();

        var handler = PrepareHandler(surveyService, profileMapper);
        var receivedMessage = TestsUtils.GetServiceBusMessage(message);

        // Act
        var result = handler.Run(receivedMessage);
        await result;

        // Assert
        result.IsCompletedSuccessfully.Should().BeTrue();

        await surveyService
            .Received()
            .CloseSurvey(Arg.Any<CaseEntity>(), Arg.Any<UserInfo>(), Arg.Any<string>());
    }

    [Fact]
    public async Task HandleMessage_MessageValidationFailed()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();

        var message = new AsbMessageDto(Guid.NewGuid(), caseEntity);
        var profileMapper = GetProfileMapper_ReturningEmployeeProfile(new[] { caseEntity.ManagerEcode! });
        var surveyService = GetSurveyService();

        var handler = PrepareHandler(surveyService, profileMapper);
        var receivedMessage = TestsUtils.GetServiceBusMessage(message);

        // Act
        var result = handler.Run(receivedMessage);
        await result;

        // Assert
        result.IsCompletedSuccessfully.Should().BeTrue();

        await surveyService
            .DidNotReceive()
            .CloseSurvey(Arg.Any<CaseEntity>(), Arg.Any<UserInfo>(), Arg.Any<string>());
    }

    private static IWorkflowSurveyService GetSurveyService()
    {
        var surveyService = Substitute.For<IWorkflowSurveyService>();
        return surveyService;
    }

    private static EndSurveyHandler PrepareHandler(
        IWorkflowSurveyService surveyService, 
        IProfileMapper profileMapper)
    {
        var logger = Substitute.For<ILogger<EndSurveyHandler>>();
        var options = Options.Create(new CosmosChangeFeedOptions());
        return new EndSurveyHandler(surveyService, profileMapper, options, logger);
    }

    private IProfileMapper GetProfileMapper_ReturningEmployeeProfile(IReadOnlyList<string> employeeCodes)
    {
        var profileMapper = Substitute.For<IProfileMapper>();
        profileMapper
            .GetEmployeeProfiles(Arg.Any<IReadOnlyList<string>>(), Arg.Any<string>())
            .Returns(employeeCodes.ToDictionary(e => e, Fake.EmployeeDetails));

        return profileMapper;
    }
}