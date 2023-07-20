using FluentValidation;
using Newtonsoft.Json;
using NSubstitute.ReturnsExtensions;
using PEXC.Case.DataAccess;
using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;
using PEXC.Case.Functions.Validators;
using PEXC.Case.Tests.Common;

namespace PEXC.Case.Functions.Tests.Validators;

public class TriggerSurveyValidatorTests
{
    [Fact]
    public async Task Validator_EntityIsNotCaseEntity_InvalidState()
    {
        // Arrange
        var entity = Substitute.For<IEntity>();
        var message = new AsbMessageDto(Guid.NewGuid(), entity);

        // Act
        var validator = new TriggerSurveyValidator(Substitute.For<ISingleCaseRepository>());
        var validationResult = await validator.ValidateAsync(message);

        // Assert
        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validator_MessageIsValid()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        var message = new AsbMessageDto(Guid.NewGuid(), caseEntity);

        var caseRepository = Substitute.For<ISingleCaseRepository>();
        caseRepository.GetCase(caseEntity.Id, caseEntity.Key).Returns(Clone(caseEntity));

        // Act
        var validator = new TriggerSurveyValidator(caseRepository);
        var validationResult = await validator.ValidateAsync(message);

        // Assert
        validationResult.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validator_NoCase()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        var message = new AsbMessageDto(Guid.NewGuid(), caseEntity);

        var caseRepository = Substitute.For<ISingleCaseRepository>();
        caseRepository.GetCase(caseEntity.Id, caseEntity.Key).ReturnsNull();

        // Act
        var validator = new TriggerSurveyValidator(caseRepository);
        var validationResult = await validator.ValidateAsync(message);

        // Assert
        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validator_EndDateNull()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        var message = new AsbMessageDto(Guid.NewGuid(), caseEntity);

        var current = Clone(caseEntity);
        current.EndDate = null;
        var caseRepository = Substitute.For<ISingleCaseRepository>();
        caseRepository.GetCase(caseEntity.Id, caseEntity.Key).Returns(current);

        // Act
        var validator = new TriggerSurveyValidator(caseRepository);
        var validationResult = await validator.ValidateAsync(message);

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Count.Should().Be(1);
        validationResult.Errors.First().Severity.Should().Be(Severity.Error);
    }
    
    [Fact]
    public async Task Validator_EtagHasChanged()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        var message = new AsbMessageDto(Guid.NewGuid(), caseEntity);

        var current = Clone(caseEntity);
        current.ETag = "new-tag";
        var caseRepository = Substitute.For<ISingleCaseRepository>();
        caseRepository.GetCase(caseEntity.Id, caseEntity.Key).Returns(current);

        // Act
        var validator = new TriggerSurveyValidator(caseRepository);
        var validationResult = await validator.ValidateAsync(message);

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Count.Should().Be(1);
        validationResult.Errors.First().Severity.Should().Be(Severity.Info);
    }

    [Fact]
    public async Task Validator_TimestampHasChanged()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        var message = new AsbMessageDto(Guid.NewGuid(), caseEntity);

        var current = Clone(caseEntity);
        current.Timestamp = DateTime.UtcNow.Ticks;
        var caseRepository = Substitute.For<ISingleCaseRepository>();
        caseRepository.GetCase(caseEntity.Id, caseEntity.Key).Returns(current);

        // Act
        var validator = new TriggerSurveyValidator(caseRepository);
        var validationResult = await validator.ValidateAsync(message);

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Count.Should().Be(1);
        validationResult.Errors.First().Severity.Should().Be(Severity.Info);
    }

    public static T Clone<T>(T source)
    {
        var serialized = JsonConvert.SerializeObject(source);
        return JsonConvert.DeserializeObject<T>(serialized)!;
    }
}