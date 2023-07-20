using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;
using PEXC.Case.Functions.Validators;
using PEXC.Case.Services;
using PEXC.Case.Tests.Common;

namespace PEXC.Case.Functions.Tests.Validators;

public class StartSurveyValidatorTests
{
    [Fact]
    public async Task Validator_MessageIsValid()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
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

    [Fact]
    public async Task Validator_InvalidItemStage_ValidationFailed()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        caseEntity.ItemStage = CaseState.Published;

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
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors
            .Should()
            .Contain(v => v.PropertyName == "Entity.ItemStage");
    }

    [Theory]
    [InlineData(nameof(CaseEntity.CaseName))]
    [InlineData(nameof(CaseEntity.UniqueId))]
    [InlineData(nameof(CaseEntity.PrimaryCapability))]
    [InlineData(nameof(CaseEntity.ManagerEcode))]
    [InlineData(nameof(CaseEntity.EndDate))]
    public async Task Validator_RequiredFieldNotProvided_ValidationFailed(string propertyName)
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        caseEntity.GetType().GetProperty(propertyName)!.SetValue(caseEntity, null);
        var message = new AsbMessageDto(Guid.NewGuid(), caseEntity);

        // Act
        var validator = GetValidator();
        var validationResult = await validator.ValidateAsync(message);

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors
            .Should()
            .Contain(v => v.PropertyName == $"Entity.{propertyName}");
    }

    [Fact]
    public async Task Validator_EmployeeProfileNotFound_ValidationFailed()
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
            .Contain(v => v.PropertyName == "Entity.ManagerEcode");
    }

    private static IProfileMapper GetProfileMapper_ReturningEmployeeProfile(IReadOnlyList<string> employeeCodes)
    {
        var profileMapper = Substitute.For<IProfileMapper>();
        profileMapper
            .GetEmployeeProfiles(Arg.Any<IReadOnlyList<string>>(), Arg.Any<string>())
            .Returns(employeeCodes.ToDictionary(e => e, Fake.EmployeeDetails));

        return profileMapper;
    }

    private static StartSurveyValidator GetValidator(IProfileMapper? profileMapper = null) =>
        new(profileMapper ?? Substitute.For<IProfileMapper>(), "CorrelationId");
}