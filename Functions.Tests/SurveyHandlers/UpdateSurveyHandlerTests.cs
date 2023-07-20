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

public class UpdateSurveyHandlerTests
{
    [Fact]
    public async Task HandleMessage_SurveyUpdated()
    {
        // Arrange
        var caseEntity = GetCaseEntity();

        var message = new AsbMessageDto(Guid.NewGuid(), caseEntity);
        var profileMapper =
            GetProfileMapper_ReturningEmployeeProfile(new[]
                { caseEntity.ManagerEcode!, caseEntity.BillingPartnerEcode! });

        var surveyService = GetSurveyService();
        surveyService.GetCase(caseEntity.Id, caseEntity.Key).Returns(caseEntity);

        var handler = PrepareHandler(surveyService, profileMapper);
        var receivedMessage = TestsUtils.GetServiceBusMessage(message);

        // Act
        var result = handler.Run(receivedMessage);
        await result;

        // Assert
        result.IsCompletedSuccessfully.Should().BeTrue();

        await surveyService
            .Received()
            .UpdateSurvey(Arg.Any<CaseEntity>(), Arg.Any<UserInfo>(), Arg.Any<string>());
    }

    [Fact]
    public async Task HandleMessage_MessageValidationFailed()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        caseEntity.Permissions = new List<Permission>
        {
            new("permId", caseEntity.ManagerEcode!, PermissionScope.User, PermissionType.SurveyAccess, DateTime.Now, true)
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
            .DidNotReceive()
            .UpdateSurvey(Arg.Any<CaseEntity>(), Arg.Any<UserInfo>(), Arg.Any<string>());
    }

    [Fact]
    public async Task HandleMessage_CaseNotFound_MessageValidationFailed()
    {
        // Arrange
        var caseEntity = GetCaseEntity();
        var message = new AsbMessageDto(Guid.NewGuid(), caseEntity);
        var profileMapper =
            GetProfileMapper_ReturningEmployeeProfile(new[]
                { caseEntity.ManagerEcode!, caseEntity.BillingPartnerEcode! });

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
            .UpdateSurvey(Arg.Any<CaseEntity>(), Arg.Any<UserInfo>(), Arg.Any<string>());
    }

    private static CaseEntity GetCaseEntity()
    {
        var caseEntity = Fake.CaseEntity();
        caseEntity.ItemStage = CaseState.SurveyOpened;

        caseEntity.Permissions = new List<Permission>
        {
            new("permId", "eCode123", PermissionScope.User, PermissionType.SurveyAccess, DateTime.Now, true)
        };

        caseEntity.SharePointDirectory = new SharePointDirectoryEntity()
        {
            DirectoryId = "directoryId",
            DriveId = "driveId"
        };

        return caseEntity;
    }

    private static IWorkflowSurveyService GetSurveyService()
    {
        var surveyService = Substitute.For<IWorkflowSurveyService>();
        return surveyService;
    }

    private static UpdateSurveyHandler PrepareHandler(
        IWorkflowSurveyService surveyService,
        IProfileMapper profileMapper)
    {
        var logger = Substitute.For<ILogger<UpdateSurveyHandler>>();
        var options = Options.Create(new CosmosChangeFeedOptions());
        return new UpdateSurveyHandler(profileMapper, surveyService, options, logger);
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