using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;
using PEXC.Case.Functions.Validators;
using PEXC.Case.Services;
using PEXC.Case.Tests.Common;

namespace PEXC.Case.Functions.Tests.Validators;

public class UpdateSurveyValidatorTests
{
    [Theory]
    [InlineData(CaseState.SurveyOpened)]
    [InlineData(CaseState.Deleted)]
    public async Task Validator_MessageIsValid(CaseState caseState)
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        caseEntity.ItemStage = caseState;
        caseEntity.Permissions = new List<Permission>
        {
            new("permId", "eCode123", PermissionScope.User, PermissionType.SurveyAccess, DateTime.Now, true)
        };
        caseEntity.SharePointDirectory = new SharePointDirectoryEntity()
        {
            DirectoryId = "directoryId",
            DriveId = "driveId"
        };

        var message = new AsbMessageDto(Guid.NewGuid(), caseEntity);
        var profileMapper =
            GetProfileMapper_ReturningEmployeeProfile(
                new[]
                {
                    caseEntity.ManagerEcode!,
                    caseEntity.BillingPartnerEcode!
                });

        // Act
        var validator = GetValidator(profileMapper);
        var validationResult = await validator.ValidateAsync(message);

        // Assert
        validationResult.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(CaseState.New)]
    [InlineData(CaseState.SurveyOpening)]
    [InlineData(CaseState.SurveyClosing)]
    [InlineData(CaseState.SurveyClosed)]
    [InlineData(CaseState.Published)]
    public async Task Validator_InvalidItemStage_ValidationFailed(CaseState caseState)
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        caseEntity.ItemStage = caseState;

        var message = new AsbMessageDto(Guid.NewGuid(), caseEntity);
        var profileMapper =
            GetProfileMapper_ReturningEmployeeProfile(
                new[]
                {
                    caseEntity.ManagerEcode!
                });

        // Act
        var validator = GetValidator(profileMapper);
        var validationResult = await validator.ValidateAsync(message);

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors
            .Should()
            .Contain(v => v.PropertyName == "Entity.ItemStage");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Validator_CaseManagerProfileNotFound_ValidationFailed(bool checkTerminated)
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        caseEntity.ItemStage = CaseState.SurveyOpened;
        caseEntity.Permissions = new List<Permission>
        {
            new("permId", "eCode123", PermissionScope.User, PermissionType.SurveyAccess, DateTime.Now, true)
        };

        var profileMapper = checkTerminated
            ? GetProfileMapper_ReturningEmployeeProfile(
                new[]
                {
                    caseEntity.ManagerEcode!
                }, true)
            : Substitute.For<IProfileMapper>();

        var message = new AsbMessageDto(Guid.NewGuid(), caseEntity);

        // Act
        var validator = GetValidator(profileMapper);
        var validationResult = await validator.ValidateAsync(message);

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors
            .Should()
            .Contain(v => v.PropertyName == "Entity.ManagerEcode");
    }

    private IProfileMapper GetProfileMapper_ReturningEmployeeProfile(IReadOnlyList<string> employeeCodes, bool isTerminated = false)
    {
        var profileMapper = Substitute.For<IProfileMapper>();
        profileMapper
            .GetEmployeeProfiles(Arg.Any<IReadOnlyList<string>>(), Arg.Any<string>())
            .Returns(employeeCodes.ToDictionary(e => e, e => Fake.EmployeeDetails(e) with {IsTerminated = isTerminated }));

        return profileMapper;
    }

    private UpdateSurveyValidator GetValidator(IProfileMapper? profileMapper = null) =>
        new(profileMapper ?? Substitute.For<IProfileMapper>(), "CorrelationId");
}