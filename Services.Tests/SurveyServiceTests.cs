using Microsoft.Extensions.Logging;
using NSubstitute.ReturnsExtensions;
using PEXC.Case.DataAccess;
using PEXC.Case.DataContracts.V1.CaseForms;
using PEXC.Case.Domain;
using PEXC.Case.Services.Mapping;
using PEXC.Case.Tests.Common;
using PEXC.Common.BaseApi.User;
using PEXC.Common.BaseApi.ErrorHandling;

namespace PEXC.Case.Services.Tests;

public class SurveyServiceTests : MappingAwareTestsBase<MainProfile>
{
    [Fact]
    public async Task GetSurvey_WhenSurveyAvailable_ReturnsMappedSurveyFromRepo()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        caseEntity.ItemStage = CaseState.SurveyOpened;
        var expectedSurvey = Fake.SurveyDtoFromEntity(caseEntity);

        var caseRepo = Substitute.For<ISingleCaseRepository>();
        caseRepo.GetCase(caseEntity.Id, caseEntity.Key).Returns(caseEntity);

        var userProvider = Substitute.For<IUserProvider>();
        userProvider
            .GetCurrentUserEcode()
            .Returns(caseEntity.ManagerEcode);

        var surveyService = GetService(caseRepo, userProvider);

        // Act
        var result = await surveyService.GetSurvey(caseEntity.Id, caseEntity.Key);

        // Assert
        await caseRepo.Received(1).GetCase(caseEntity.Id, caseEntity.Key);
        result.Should().BeEquivalentTo(expectedSurvey);
    }

    [Fact]
    public async Task GetSurvey_WhenCaseNotExists_ThrowsException()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();

        var caseRepo = Substitute.For<ISingleCaseRepository>();
        caseRepo.GetCase(caseEntity.Id, caseEntity.Key).ReturnsNull();

        var surveyService = GetService(caseRepo);

        // Act
        var call = () => surveyService.GetSurvey(caseEntity.Id, caseEntity.Key);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(call);
        await caseRepo.Received(1).GetCase(caseEntity.Id, caseEntity.Key);
    }

    [Fact]
    public async Task GetSurvey_WhenUserIsNotCaseManager_ThrowsException()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        caseEntity.ItemStage = CaseState.SurveyOpened;

        var caseRepo = Substitute.For<ISingleCaseRepository>();
        caseRepo.GetCase(caseEntity.Id, caseEntity.Key).Returns(caseEntity);

        var service = GetService(caseRepo);

        // Act
        var call = () => service.GetSurvey(caseEntity.Id, caseEntity.Key);

        // Assert
        await call.Should().ThrowAsync<NotFoundException>();
        await caseRepo
            .Received(1)
            .GetCase(caseEntity.Id, caseEntity.Key);
        await caseRepo
            .DidNotReceive()
            .UpdateCase(Arg.Any<CaseEntity>());
    }

    [Theory]
    [InlineData(CaseState.New)]
    [InlineData(CaseState.SurveyOpening)]
    [InlineData(CaseState.SurveyClosing)]
    [InlineData(CaseState.SurveyClosed)]
    [InlineData(CaseState.Published)]
    [InlineData(CaseState.Deleted)]
    public async Task GetSurvey_WhenSurveyIsNotOpen_ThrowsException(CaseState itemStage)
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        caseEntity.ItemStage = itemStage;

        var caseRepo = Substitute.For<ISingleCaseRepository>();
        caseRepo.GetCase(caseEntity.Id, caseEntity.Key).Returns(caseEntity);

        var userProvider = Substitute.For<IUserProvider>();
        userProvider
            .GetCurrentUserEcode()
            .Returns(caseEntity.ManagerEcode);

        var service = GetService(caseRepo, userProvider);

        // Act
        var call = () => service.GetSurvey(caseEntity.Id, caseEntity.Key);

        // Assert
        await call.Should().ThrowAsync<NotFoundException>();
        await caseRepo
            .Received(1)
            .GetCase(caseEntity.Id, caseEntity.Key);
        await caseRepo
            .DidNotReceive()
            .UpdateCase(Arg.Any<CaseEntity>());
    }

    [Fact]
    public async Task SaveSurvey_ThrowsErrorWhenCaseIsNotFound()
    {
        // Arrange
        var surveyDto = CreateSurveyDto();

        var caseRepo = Substitute.For<ISingleCaseRepository>();
        caseRepo.GetCase(surveyDto.Id, surveyDto.Key).ReturnsNull();

        var service = GetService(caseRepo);

        // Act
        var call = () => service.SaveSurvey(surveyDto);

        // Assert
        await call.Should().ThrowAsync<NotFoundException>();
        await caseRepo
            .Received(1)
            .GetCase(surveyDto.Id, surveyDto.Key);
    }

    [Fact]
    public async Task SaveSurvey_AddsCurrentUserInfoToModifiedBy()
    {
        // Arrange
        var originalEntity = Fake.CaseEntity();
        originalEntity.ItemStage = CaseState.SurveyOpened;
        var surveyDto = Fake.SurveyDtoFromEntity(originalEntity);

        var caseRepo = Substitute.For<ISingleCaseRepository>();
        caseRepo.GetCase(originalEntity.Id, originalEntity.Key).Returns(originalEntity);

        var userProvider = Substitute.For<IUserProvider>();
        userProvider
            .GetCurrentUserEcode()
            .Returns(originalEntity.ManagerEcode);
        userProvider
            .GetCurrentUserFullName()
            .Returns("user-name");
        userProvider
            .GetCurrentUserId()
            .Returns("user-id");

        var service = GetService(caseRepo, userProvider);

        // Act
        await service.SaveSurvey(surveyDto);

        // Assert
        await caseRepo
            .Received()
            .UpdateCase(Arg.Is<CaseEntity>(c =>
                c.Id == surveyDto.Id &&
                c.Key == surveyDto.Key &&
                c.ModifiedBy!.UserType == UserType.User &&
                c.ModifiedBy.UserEcode == originalEntity.ManagerEcode &&
                c.ModifiedBy.DisplayName == "user-name" &&
                c.ModifiedBy.UserId == "user-id"));
    }

    [Fact]
    public async Task SaveSurvey_OnlyTargetDetailsWillBeChanged()
    {
        // Arrange
        var originalEntity = Fake.CaseEntity();
        originalEntity.ItemStage = CaseState.SurveyOpened;
        var surveyDto = Fake.SurveyDtoFromEntity(originalEntity);
        surveyDto.SurveyCaseDetailsSection!.CaseName = "NewName";
        surveyDto.SurveyTargetDetailsSection!.TargetDetailsAndFinalDocumentsSection!.TargetName = "NewTargetName";

        var caseRepo = Substitute.For<ISingleCaseRepository>();
        caseRepo.GetCase(originalEntity.Id, originalEntity.Key).Returns(originalEntity);

        var userProvider = Substitute.For<IUserProvider>();
        userProvider
            .GetCurrentUserEcode()
            .Returns(originalEntity.ManagerEcode);

        var service = GetService(caseRepo, userProvider);

        // Act
        await service.SaveSurvey(surveyDto);

        // Assert
        await caseRepo
            .Received()
            .UpdateCase(Arg.Is<CaseEntity>(c =>
                c.Id == surveyDto.Id &&
                c.Key == surveyDto.Key &&
                c.TargetName == "NewTargetName" &&
                c.CaseName == originalEntity.CaseName));
    }

    [Fact]
    public async Task SaveSurvey_WhenUserIsNotCaseManager_ThrowsException()
    {
        // Arrange
        var originalEntity = Fake.CaseEntity();
        originalEntity.ItemStage = CaseState.SurveyOpened;
        var surveyDto = Fake.SurveyDtoFromEntity(originalEntity);

        var caseRepo = Substitute.For<ISingleCaseRepository>();
        caseRepo.GetCase(originalEntity.Id, originalEntity.Key).Returns(originalEntity);

        var service = GetService(caseRepo);

        // Act
        var call = () => service.SaveSurvey(surveyDto);

        // Assert
        await call.Should().ThrowAsync<NotFoundException>();
        await caseRepo
            .Received(1)
            .GetCase(originalEntity.Id, originalEntity.Key);
        await caseRepo
            .DidNotReceive()
            .UpdateCase(Arg.Any<CaseEntity>());
    }

    [Theory]
    [InlineData(CaseState.New)]
    [InlineData(CaseState.SurveyOpening)]
    [InlineData(CaseState.SurveyClosing)]
    [InlineData(CaseState.SurveyClosed)]
    [InlineData(CaseState.Published)]
    [InlineData(CaseState.Deleted)]
    public async Task SaveSurvey_WhenSurveyIsNotOpen_ThrowsException(CaseState itemStage)
    {
        // Arrange
        var originalEntity = Fake.CaseEntity();
        originalEntity.ItemStage = itemStage;
        var surveyDto = Fake.SurveyDtoFromEntity(originalEntity);

        var caseRepo = Substitute.For<ISingleCaseRepository>();
        caseRepo.GetCase(originalEntity.Id, originalEntity.Key).Returns(originalEntity);

        var userProvider = Substitute.For<IUserProvider>();
        userProvider
            .GetCurrentUserEcode()
            .Returns(originalEntity.ManagerEcode);

        var service = GetService(caseRepo, userProvider);

        // Act
        var call = () => service.SaveSurvey(surveyDto);

        // Assert
        await call.Should().ThrowAsync<NotFoundException>();
        await caseRepo
            .Received(1)
            .GetCase(originalEntity.Id, originalEntity.Key);
        await caseRepo
            .DidNotReceive()
            .UpdateCase(Arg.Any<CaseEntity>());
    }

    [Fact]
    public async Task SubmitSurvey_SetItemStageToSurveyClosing()
    {
        // Arrange
        var originalEntity = Fake.CaseEntity();
        originalEntity.ItemStage = CaseState.SurveyOpened;
        var surveyDto = Fake.SurveyDtoFromEntity(originalEntity);

        var caseRepo = Substitute.For<ISingleCaseRepository>();
        caseRepo.GetCase(originalEntity.Id, originalEntity.Key).Returns(originalEntity);

        var userProvider = Substitute.For<IUserProvider>();
        userProvider
            .GetCurrentUserEcode()
            .Returns(originalEntity.ManagerEcode);

        var service = GetService(caseRepo, userProvider);

        // Act
        await service.SubmitSurvey(surveyDto);

        // Assert
        await caseRepo
            .Received()
            .UpdateCase(Arg.Is<CaseEntity>(c =>
                c.Id == surveyDto.Id &&
                c.Key == surveyDto.Key &&
                c.ItemStage == CaseState.SurveyClosing));
    }

    [Fact]
    public async Task SubmitSurvey_WhenUserIsNotCaseManager_ThrowsException()
    {
        // Arrange
        var originalEntity = Fake.CaseEntity();
        originalEntity.ItemStage = CaseState.SurveyOpened;
        var surveyDto = Fake.SurveyDtoFromEntity(originalEntity);

        var caseRepo = Substitute.For<ISingleCaseRepository>();
        caseRepo.GetCase(originalEntity.Id, originalEntity.Key).Returns(originalEntity);

        var service = GetService(caseRepo);

        // Act
        var call = () => service.SubmitSurvey(surveyDto);

        // Assert
        await call.Should().ThrowAsync<NotFoundException>();
        await caseRepo
            .Received(1)
            .GetCase(originalEntity.Id, originalEntity.Key);
        await caseRepo
            .DidNotReceive()
            .UpdateCase(Arg.Any<CaseEntity>());
    }

    [Theory]
    [InlineData(CaseState.New)]
    [InlineData(CaseState.SurveyOpening)]
    [InlineData(CaseState.SurveyClosing)]
    [InlineData(CaseState.SurveyClosed)]
    [InlineData(CaseState.Published)]
    [InlineData(CaseState.Deleted)]
    public async Task SubmitSurvey_WhenSurveyIsNotOpen_ThrowsException(CaseState itemStage)
    {
        // Arrange
        var originalEntity = Fake.CaseEntity();
        originalEntity.ItemStage = itemStage;
        var surveyDto = Fake.SurveyDtoFromEntity(originalEntity);

        var caseRepo = Substitute.For<ISingleCaseRepository>();
        caseRepo.GetCase(originalEntity.Id, originalEntity.Key).Returns(originalEntity);

        var userProvider = Substitute.For<IUserProvider>();
        userProvider
            .GetCurrentUserEcode()
            .Returns(originalEntity.ManagerEcode);

        var service = GetService(caseRepo, userProvider);

        // Act
        var call = () => service.SubmitSurvey(surveyDto);

        // Assert
        await call.Should().ThrowAsync<NotFoundException>();
        await caseRepo
            .Received(1)
            .GetCase(originalEntity.Id, originalEntity.Key);
        await caseRepo
            .DidNotReceive()
            .UpdateCase(Arg.Any<CaseEntity>());
    }

    private static SurveyDto CreateSurveyDto() 
        => new SurveyDto("case-id", "case-key", "case-code", RelationshipType.NonRetainer);


    private SurveyService GetService(ISingleCaseRepository caseRepository, IUserProvider? userProvider = null)
    {
        var logger = Substitute.For<ILogger<SurveyService>>();
        userProvider ??= Substitute.For<IUserProvider>();

        return new SurveyService(caseRepository, Mapper, userProvider, logger);
    }
}