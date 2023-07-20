using PEXC.Case.DataAccess;
using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;
using PEXC.Case.Functions.Validators;
using PEXC.Case.Tests.Common;

namespace PEXC.Case.Functions.Tests.Validators;

public class ScheduleSurveyValidatorTests
{

    [Fact]
    public async Task Validator_MessageIsValid()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        var message = new AsbMessageDto(Guid.NewGuid(), caseEntity);
        
        // Act
        var validator = new ScheduleSurveyValidator();
        var validationResult = await validator.ValidateAsync(message);

        // Assert
        validationResult.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validator_EntityIsNotCaseEntity_InvalidState()
    {
        // Arrange
        var entity = Substitute.For<IEntity>();
        var message = new AsbMessageDto(Guid.NewGuid(), entity);
        
        // Act
        var validator = new ScheduleSurveyValidator();
        var validationResult = await validator.ValidateAsync(message);

        // Assert
        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validator_EndDateNull_InvalidState()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        caseEntity.EndDate = null;

        var message = new AsbMessageDto(Guid.NewGuid(), caseEntity);
        
        // Act
        var validator = new ScheduleSurveyValidator();
        var validationResult = await validator.ValidateAsync(message);

        // Assert
        validationResult.IsValid.Should().BeFalse();
    }


    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Validator_InvalidTimeStamp_InvalidState(long timestamp)
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        caseEntity.Timestamp = timestamp;

        var message = new AsbMessageDto(Guid.NewGuid(), caseEntity);

        // Act
        var validator = new ScheduleSurveyValidator();
        var validationResult = await validator.ValidateAsync(message);

        // Assert
        validationResult.IsValid.Should().BeFalse();
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Validator_InvalidEtag_InvalidState(string etag)
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        caseEntity.ETag = etag;

        var message = new AsbMessageDto(Guid.NewGuid(), caseEntity);

        // Act
        var validator = new ScheduleSurveyValidator();
        var validationResult = await validator.ValidateAsync(message);

        // Assert
        validationResult.IsValid.Should().BeFalse();
    }
}