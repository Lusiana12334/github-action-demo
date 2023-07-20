using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;
using PEXC.Case.Functions.Validators;
using PEXC.Case.Services;
using PEXC.Case.Tests.Common;

namespace PEXC.Case.Functions.Tests.Validators;

public class EndSurveyValidatorTests
{
    [Fact]
    public async Task Validator_MessageIsValid()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        caseEntity.ItemStage = CaseState.SurveyClosing;
        caseEntity.SharePointDirectory = new SharePointDirectoryEntity()
        {
            DirectoryId = "directoryId",
            DriveId = "driveId"
        };

        var message = new AsbMessageDto(Guid.NewGuid(), caseEntity);
        var profileMapper = GetProfileMapper(new[] { caseEntity.ManagerEcode! });

        // Act
        var validator = GetValidator(profileMapper);
        var validationResult = await validator.ValidateAsync(message);

        // Assert
        validationResult.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validator_InvalidItemStage_ValidationFailed()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        caseEntity.ItemStage = CaseState.SurveyClosed;

        var message = new AsbMessageDto(Guid.NewGuid(), caseEntity);

        // Act
        var validator = GetValidator();
        var validationResult = await validator.ValidateAsync(message);

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors
            .Should()
            .Contain(v => v.PropertyName == "Entity.ItemStage");
    }

    [Fact]
    public async Task Validator_DriveIdNotProvided_ValidationFailed()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        caseEntity.SharePointDirectory = new SharePointDirectoryEntity()
        {
            DirectoryId = "directoryId"
        };

        var message = new AsbMessageDto(Guid.NewGuid(), caseEntity);

        // Act
        var validator = GetValidator();
        var validationResult = await validator.ValidateAsync(message);

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors
            .Should()
            .Contain(v => v.PropertyName == "Entity.SharePointDirectory.DriveId");
    }

    [Fact]
    public async Task Validator_DirectoryIdNotProvided_ValidationFailed()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        caseEntity.SharePointDirectory = new SharePointDirectoryEntity()
        {
            DriveId = "driveId"
        };

        var message = new AsbMessageDto(Guid.NewGuid(), caseEntity);

        // Act
        var validator = GetValidator();
        var validationResult = await validator.ValidateAsync(message);

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors
            .Should()
            .Contain(v => v.PropertyName == "Entity.SharePointDirectory.DirectoryId");
    }

    [Fact]
    public async Task Validator_SharePointDirectoryNotProvided_ValidationFailed()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        var message = new AsbMessageDto(Guid.NewGuid(), caseEntity);

        // Act
        var validator = GetValidator();
        var validationResult = await validator.ValidateAsync(message);

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors
            .Should()
            .Contain(v => v.PropertyName == "Entity.SharePointDirectory");
    }

    [Fact]
    public async Task Validator_CaseManagerEcodeNotProvided_ValidationFailed()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        caseEntity.ManagerEcode = null;

        var message = new AsbMessageDto(Guid.NewGuid(), caseEntity);

        // Act
        var validator = GetValidator();
        var validationResult = await validator.ValidateAsync(message);

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors
            .Should()
            .Contain(v => v.PropertyName == "Entity.ManagerEcode");
    }

    [Fact]
    public async Task Validator_CaseManagerProfileNotFound_ValidationFailed()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        caseEntity.ItemStage = CaseState.SurveyClosing;
        caseEntity.SharePointDirectory = new SharePointDirectoryEntity()
        {
            DirectoryId = "directoryId",
            DriveId = "driveId"
        };

        var message = new AsbMessageDto(Guid.NewGuid(), caseEntity);

        // Act
        var validator = GetValidator();
        var validationResult = await validator.ValidateAsync(message);

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors
            .Should()
            .Contain(v => v.PropertyName == "Entity.ManagerEcode");
    }

    private static IProfileMapper GetProfileMapper(IReadOnlyList<string> employeeCodes)
    {
        var profileMapper = Substitute.For<IProfileMapper>();
        profileMapper
            .GetEmployeeProfiles(Arg.Any<IReadOnlyList<string>>(), Arg.Any<string>())
            .Returns(employeeCodes.ToDictionary(e => e, Fake.EmployeeDetails));

        return profileMapper;
    }

    private static EndSurveyValidator GetValidator(IProfileMapper? profileMapper = null) =>
        new(profileMapper ?? Substitute.For<IProfileMapper>(), "CorrelationId");
}