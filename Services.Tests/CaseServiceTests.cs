using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute.ReturnsExtensions;
using PEXC.Case.DataAccess;
using PEXC.Case.DataContracts.V1;
using PEXC.Case.DataContracts.V1.CaseForms;
using PEXC.Case.Domain;
using PEXC.Case.Services.Infrastructure;
using PEXC.Case.Services.Mapping;
using PEXC.Case.Services.Mapping.EmployeeProfile;
using PEXC.Case.Services.Mapping.FieldMasking;
using PEXC.Case.Services.Mapping.Taxonomy;
using PEXC.Case.Tests.Common;
using PEXC.Common.BaseApi.User;
using PEXC.Common.BaseApi.ErrorHandling;
using PEXC.Common.BaseApi.Profile;

namespace PEXC.Case.Services.Tests;

public class CaseServiceTests : MappingAwareTestsBase<MainProfile>
{
    [Fact]
    public async Task IsCaseUnique_WhenCaseExistsAndIsTheSameInstance_ReturnsTrue()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity("Id");
        var caseInstance = new CaseEditDto("Id", "Key", "CaseCode", default, default)
        {
            CaseDetailsSection = new CaseDetailsSectionDto { CaseName = "CaseName" }
        };
        var caseRepo = Substitute.For<ISingleCaseRepository>();
        caseRepo
            .GetRetainerCaseByCaseCodeAndName(caseInstance.CaseCode, caseInstance.CaseDetailsSection!.CaseName!)
            .Returns(caseEntity);

        var service = CreateService(caseRepo);

        // Act
        var result = await service.IsCaseUnique(
            caseInstance.CaseCode, caseInstance.CaseDetailsSection!.CaseName!, caseInstance.Id);

        // Assert
        result
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task IsCaseUnique_WhenCaseDoesNotExist_ReturnsTrue()
    {
        // Arrange
        var caseInstance = new CaseEditDto("Id", "Key", "CaseCode", default, default)
        {
            CaseDetailsSection = new CaseDetailsSectionDto { CaseName = "CaseName" }
        };
        var caseRepo = Substitute.For<ISingleCaseRepository>();
        caseRepo
            .GetRetainerCaseByCaseCodeAndName(
                caseInstance.CaseCode, caseInstance.CaseDetailsSection!.CaseName!)
            .Returns((CaseEntity?)null);

        var service = CreateService(caseRepo);

        // Act
        var result = await service.IsCaseUnique(
            caseInstance.CaseCode, caseInstance.CaseDetailsSection!.CaseName!, caseInstance.Id);

        // Assert
        result
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task IsCaseUnique_WhenCaseExistsAndIsDifferentInstance_ReturnsFalse()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity("Id2");
        var caseInstance = new CaseEditDto("Id", "Key", "CaseCode", default, default)
        {
            CaseDetailsSection = new CaseDetailsSectionDto { CaseName = "CaseName" }
        };
        var caseRepo = Substitute.For<ISingleCaseRepository>();
        caseRepo
            .GetRetainerCaseByCaseCodeAndName(caseInstance.CaseCode, caseInstance.CaseDetailsSection!.CaseName!)
            .Returns(caseEntity);

        var service = CreateService(caseRepo);

        // Act
        var result = await service.IsCaseUnique(
            caseInstance.CaseCode, caseInstance.CaseDetailsSection!.CaseName!, caseInstance.Id);

        // Assert
        result
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task GetCase_ThrowsErrorWhenCaseIsNotFound()
    {
        var caseRepo = Substitute.For<ISingleCaseRepository>();
        caseRepo.GetCase(nameof(CaseEditDto.Id), nameof(CaseEditDto.Key)).ReturnsNull();

        var service = CreateService(caseRepo);

        // Act
        var act = () => service.GetCase(nameof(CaseEditDto.Id), nameof(CaseEditDto.Key));

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        await caseRepo.Received(1).GetCase(nameof(CaseEditDto.Id), nameof(CaseEditDto.Key));
    }

    [Fact]
    public async Task GetCase_ThrowsErrorWhenCaseIsDeleted()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity(nameof(CaseEditDto.Id), RelationshipType.Retainer);
        caseEntity.ItemStage = CaseState.Deleted;

        var caseRepo = Substitute.For<ISingleCaseRepository>();
        caseRepo.GetCase(caseEntity.Id, caseEntity.Key).Returns(caseEntity);

        var service = CreateService(caseRepo);

        // Act
        var act = () => service.GetCase(caseEntity.Id, caseEntity.Key);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        await caseRepo.Received(1).GetCase(caseEntity.Id, caseEntity.Key);
    }

    [Theory]
    [InlineData(CaseState.New, null)]
    [InlineData(CaseState.SurveyOpening, null)]
    [InlineData(CaseState.SurveyOpened, null)]
    [InlineData(CaseState.SurveyClosing, null)]
    [InlineData(CaseState.SurveyClosed, false)]
    [InlineData(CaseState.Published, true)]
    public async Task GetCase_ReturnsCases(CaseState itemStage, bool? published)
    {
        // Arrange
        var caseEntity = Fake.CaseEntity(nameof(CaseEditDto.Id), RelationshipType.Retainer);
        caseEntity.ItemStage = itemStage;
        var service = CreateService(SetupCaseRepository(caseEntity));

        // Act
        var caseEditDto = await service.GetCase(caseEntity.Id, caseEntity.Key);

        // Assert
        caseEditDto.Id.Should().Be(nameof(CaseEditDto.Id));
        caseEditDto.Published.Should().Be(published);
    }

    [Fact]
    public async Task AddCase_AddsCurrentUserInfoToModifiedByAndCreatedBy()
    {
        // Arrange
        var originalEntity = Fake.CaseEntity();
        var caseDto = Fake.CaseCreateDtoFromEntity(originalEntity);

        var caseRepo = Substitute.For<ISingleCaseRepository>();
        caseRepo.AddCase(Arg.Any<CaseEntity>()).ReturnsForAnyArgs(originalEntity);

        var userProvider = CreateUserProvider();

        var service = CreateService(caseRepo, userProvider);

        // Act
        await service.AddCase(caseDto);

        // Assert
        await caseRepo
            .Received()
            .AddCase(Arg.Is<CaseEntity>(c =>
                c.CaseCode == caseDto.CaseCode &&
                c.ModifiedBy!.UserType == UserType.User &&
                c.ModifiedBy.UserEcode == "user-ecode" &&
                c.ModifiedBy.DisplayName == "user-name" &&
                c.ModifiedBy.UserId == "user-id" &&
                c.CreatedBy!.UserType == UserType.User &&
                c.CreatedBy.UserEcode == "user-ecode" &&
                c.CreatedBy.DisplayName == "user-name" &&
                c.CreatedBy.UserId == "user-id"));
    }

    [Fact]
    public async Task AddCase_ClearIdAndSetInitialFields()
    {
        // Arrange
        var originalEntity = Fake.CaseEntity();
        var caseCreateDto = Fake.CaseCreateDtoFromEntity(originalEntity);

        var caseRepo = Substitute.For<ISingleCaseRepository>();
        caseRepo.AddCase(Arg.Any<CaseEntity>()).ReturnsForAnyArgs(originalEntity);

        var service = CreateService(caseRepo);

        // Act
        await service.AddCase(caseCreateDto);

        Guid _;
        // Assert
        await caseRepo
            .Received()
            .AddCase(Arg.Is<CaseEntity>(c =>
                c.CaseCode == caseCreateDto.CaseCode &&
                Guid.TryParse(c.Id, out _) &&
                c.ItemStage == CaseState.New &&
                c.RelationshipType == RelationshipType.Retainer));
    }

    [Fact]
    public async Task AddCase_ReturnEditCaseDto()
    {
        // Arrange
        var originalEntity = Fake.CaseEntity();
        var caseCreateDto = Fake.CaseCreateDtoFromEntity(originalEntity);
        var expected = new CaseIdentifierDto(originalEntity.Id, originalEntity.Key);

        var caseRepo = Substitute.For<ISingleCaseRepository>();
        caseRepo.AddCase(Arg.Any<CaseEntity>()).ReturnsForAnyArgs(originalEntity);

        var service = CreateService(caseRepo);

        // Act
        var result = await service.AddCase(caseCreateDto);

        // Assert
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task AddCase_TrimCaseCodeAndKey()
    {
        // Arrange
        var originalEntity = Fake.CaseEntity();
        originalEntity.CaseCode = $" {nameof(CaseEntity.CaseCode)} ";
        var caseDto = Fake.CaseCreateDtoFromEntity(originalEntity);

        var caseRepo = Substitute.For<ISingleCaseRepository>();
        caseRepo.AddCase(Arg.Any<CaseEntity>()).ReturnsForAnyArgs(originalEntity);

        var userProvider = CreateUserProvider();

        var service = CreateService(caseRepo, userProvider);

        // Act
        await service.AddCase(caseDto);

        // Assert
        await caseRepo
            .Received()
            .AddCase(Arg.Is<CaseEntity>(c =>
                c.CaseCode == caseDto.CaseCode.Trim() &&
                c.Key == caseDto.CaseCode.Trim()));
    }

    [Fact]
    public async Task UpdateCase_ThrowsErrorWhenCaseIsNotFound()
    {
        // Arrange
        var caseDto = CreateCaseDto();

        var caseRepo = Substitute.For<ISingleCaseRepository>();
        caseRepo.GetCase(caseDto.Id, caseDto.Key).ReturnsNull();

        var service = CreateService(caseRepo);

        // Act
        var act = () => service.UpdateCase(caseDto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        await caseRepo.Received(1).GetCase(caseDto.Id, caseDto.Key);
    }

    [Fact]
    public async Task UpdateCase_ForRetainer_TrimCaseCode()
    {
        // Arrange
        var caseCode = $" {nameof(CaseEntity.CaseCode)} ";
        var originalEntity = Fake.CaseEntity("", RelationshipType.Retainer);
        var caseDto = Fake.CaseEditDtoFromEntity(originalEntity) with { CaseCode = caseCode };
        var caseRepo = SetupCaseRepository(originalEntity);

        var userProvider = CreateUserProvider();

        var service = CreateService(caseRepo, userProvider);

        // Act
        await service.UpdateCase(caseDto);

        // Assert
        await caseRepo
            .Received()
            .UpdateCase(Arg.Is<CaseEntity>(c =>
                c.Id == caseDto.Id &&
                c.CaseCode == caseDto.CaseCode.Trim()));
    }

    [Fact]
    public async Task UpdateCase_AddsCurrentUserInfoToModifiedBy()
    {
        // Arrange
        var originalEntity = Fake.CaseEntity();
        var caseDto = Fake.CaseEditDtoFromEntity(originalEntity);
        var caseRepo = SetupCaseRepository(originalEntity);

        var userProvider = CreateUserProvider();

        var service = CreateService(caseRepo, userProvider);

        // Act
        await service.UpdateCase(caseDto);

        // Assert
        await caseRepo
            .Received()
            .UpdateCase(Arg.Is<CaseEntity>(c =>
                c.Id == caseDto.Id &&
                c.CaseCode == caseDto.CaseCode &&
                c.ModifiedBy!.UserType == UserType.User &&
                c.ModifiedBy.UserEcode == "user-ecode" &&
                c.ModifiedBy.DisplayName == "user-name" &&
                c.ModifiedBy.UserId == "user-id"));
    }

    [Fact]
    public async Task UpdateCase_ForNonRetainer_OnlyTargetDetailsWillBeChanged()
    {
        // Arrange
        var originalEntity = Fake.CaseEntity();
        var caseDto = Fake.CaseEditDtoFromEntity(originalEntity);
        caseDto.CaseDetailsSection!.CaseName = "NewName";
        caseDto.TargetDetailsSection!.TargetDetailsAndFinalDocumentsSection!.TargetName = "NewTargetName";

        var caseRepo = SetupCaseRepository(originalEntity);
        var service = CreateService(caseRepo);

        // Act
        await service.UpdateCase(caseDto);

        // Assert
        await caseRepo
            .Received()
            .UpdateCase(Arg.Is<CaseEntity>(c =>
                c.Id == caseDto.Id &&
                c.CaseCode == caseDto.CaseCode &&
                c.TargetName == "NewTargetName" &&
                c.CaseName == originalEntity.CaseName));
    }

    [Theory]
    [InlineData(null, CaseState.New, CaseState.New)]
    [InlineData(null, CaseState.SurveyOpening, CaseState.SurveyOpening)]
    [InlineData(null, CaseState.SurveyOpened, CaseState.SurveyOpened)]
    [InlineData(null, CaseState.SurveyClosing, CaseState.SurveyClosing)]
    [InlineData(true, CaseState.SurveyClosed, CaseState.Published)]
    [InlineData(true, CaseState.Published, CaseState.Published)]
    [InlineData(false, CaseState.Published, CaseState.SurveyClosed)]
    [InlineData(false, CaseState.SurveyClosed, CaseState.SurveyClosed)]
    public async Task UpdateCase_ChangePublishedState_ChangeItemStage(bool? published, CaseState caseState, CaseState newCaseState)
    {
        // Arrange
        var originalEntity = Fake.CaseEntity();
        originalEntity.ItemStage = caseState;

        var caseDto = Fake.CaseEditDtoFromEntity(originalEntity, published);
        var caseRepo = SetupCaseRepository(originalEntity);
        var service = CreateService(caseRepo);

        // Act
        await service.UpdateCase(caseDto);

        // Assert
        await caseRepo
            .Received()
            .UpdateCase(Arg.Is<CaseEntity>(c =>
                c.Id == caseDto.Id &&
                c.Key == caseDto.Key &&
                c.ItemStage == newCaseState));
    }

    [Theory]
    [InlineData(true, CaseState.New)]
    [InlineData(true, CaseState.SurveyClosing)]
    [InlineData(true, CaseState.SurveyOpened)]
    [InlineData(true, CaseState.SurveyOpening)]
    [InlineData(false, CaseState.New)]
    [InlineData(false, CaseState.SurveyClosing)]
    [InlineData(false, CaseState.SurveyOpened)]
    [InlineData(false, CaseState.SurveyOpening)]
    public async Task UpdateCase_ChangePublishedState_ThrowsInvalidOperationException(bool? published, CaseState caseState)
    {
        var originalEntity = Fake.CaseEntity();
        originalEntity.ItemStage = caseState;

        var caseDto = Fake.CaseEditDtoFromEntity(originalEntity, published);
        var caseRepo = SetupCaseRepository(originalEntity);
        var service = CreateService(caseRepo);

        // Act
        var act = () => service.UpdateCase(caseDto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        await caseRepo.Received(1).GetCase(caseDto.Id, caseDto.Key);
    }

    [Fact]
    public async Task UpdateCase_ForRetainer_CaseAndTargetDetailsWillBeChanged()
    {
        // Arrange
        var originalEntity = Fake.CaseEntity("", RelationshipType.Retainer);
        var caseDto = Fake.CaseEditDtoFromEntity(originalEntity);
        caseDto.CaseDetailsSection!.CaseName = "NewName";
        caseDto.TargetDetailsSection!.TargetDetailsAndFinalDocumentsSection!.TargetName = "NewTargetName";
        var caseRepo = SetupCaseRepository(originalEntity);

        var service = CreateService(caseRepo);

        // Act
        await service.UpdateCase(caseDto);

        // Assert
        await caseRepo
            .Received()
            .UpdateCase(Arg.Is<CaseEntity>(c =>
                c.Id == caseDto.Id &&
                c.CaseCode == caseDto.CaseCode &&
                c.TargetName == "NewTargetName" &&
                c.CaseName == "NewName"));
    }

    [Fact]
    public async Task UpdateCase_ThrowsErrorWhenCaseIsDeleted()
    {
        // Arrange
        var originalEntity = Fake.CaseEntity("", RelationshipType.Retainer);
        originalEntity.ItemStage = CaseState.Deleted;
        var caseDto = Fake.CaseEditDtoFromEntity(originalEntity);
        var caseRepo = SetupCaseRepository(originalEntity);
        var service = CreateService(caseRepo);

        // Act
        var act = () => service.UpdateCase(caseDto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        await caseRepo.Received(1).GetCase(caseDto.Id, caseDto.Key);
    }

    [Fact]
    public async Task DeleteCase_AddsCurrentUserInfoToModifiedBy()
    {
        // Arrange
        var originalEntity = Fake.CaseEntity("", RelationshipType.Retainer);
        var caseDto = Fake.CaseEditDtoFromEntity(originalEntity);
        var caseRepo = SetupCaseRepository(originalEntity);
        var userProvider = CreateUserProvider();
        var service = CreateService(caseRepo, userProvider);

        // Act
        await service.DeleteCase(caseDto.Id, caseDto.Key);

        // Assert
        var modifiedByPropertyName = nameof(CaseEntity.ModifiedBy).ToCamelCase();
        var itemStagePropertyName = nameof(CaseEntity.ItemStage).ToCamelCase();
        await caseRepo
            .Received()
            .PatchCase(
                caseDto.Id,
                caseDto.Key,
                Arg.Is<IReadOnlyDictionary<string, object?>>(
                    d =>
                        d.ContainsKey(modifiedByPropertyName) &&
                        ((UserInfo)d[modifiedByPropertyName]!).UserType == UserType.User &&
                        ((UserInfo)d[modifiedByPropertyName]!).UserEcode == "user-ecode" &&
                        ((UserInfo)d[modifiedByPropertyName]!).DisplayName == "user-name" &&
                        ((UserInfo)d[modifiedByPropertyName]!).UserId == "user-id" &&
                        d.ContainsKey(itemStagePropertyName)
                        && d[itemStagePropertyName]!.Equals(CaseState.Deleted)));
    }

    [Fact]
    public async Task DeleteCase_ThrowsErrorWhenCaseIsAlreadyDeleted()
    {
        // Arrange
        var originalEntity = Fake.CaseEntity("", RelationshipType.Retainer);
        originalEntity.ItemStage = CaseState.Deleted;
        var caseRepo = SetupCaseRepository(originalEntity);
        var service = CreateService(caseRepo);

        // Act
        var act = () => service.DeleteCase(originalEntity.Id, originalEntity.Key);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        await caseRepo.Received(1).GetCase(originalEntity.Id, originalEntity.Key);
        await caseRepo
            .DidNotReceive()
            .PatchCase(
                originalEntity.Id,
                originalEntity.Key,
                Arg.Any<IReadOnlyDictionary<string, object?>>());
    }

    private static CaseEditDto CreateCaseDto()
        => new("case-id", "case-key", "case-code", RelationshipType.NonRetainer, null);

    private static IUserProvider CreateUserProvider()
    {
        var userProvider = Substitute.For<IUserProvider>();
        userProvider
            .GetCurrentUserEcode()
            .Returns("user-ecode");
        userProvider
            .GetCurrentUserFullName()
            .Returns("user-name");
        userProvider
            .GetCurrentUserId()
            .Returns("user-id");
        return userProvider;
    }

    private CaseService CreateService(ISingleCaseRepository caseRepository, IUserProvider? userProvider = null)
    {
        var factory = Substitute.For<ITaxonomyServiceFactory>();
        factory
            .Create()
            .Returns(Substitute.For<ITaxonomyService>());

        var profileRepository = Substitute.For<IProfileRepository>();
        var mappingOptions = Options.Create(new CoveoMappingOptions());
        userProvider ??= Substitute.For<IUserProvider>();
        var mapper = CreateMapper(
            new CoveoItemTaxonomyMapping<CaseSearchItemDto>(factory, mappingOptions),
            new CoveoItemEmployeeProfileMapping<CaseSearchItemDto>(profileRepository, Substitute.For<ILogger<CoveoItemEmployeeProfileMapping<CaseSearchItemDto>>>()),
            new CaseSearchItemConfidentialDataMapping(Substitute.For<IFieldMaskingPolicy>()),
            new CoveoItemTaxonomyMapping<CaseManagementItemDto>(factory, mappingOptions),
            new CaseManagementItemEmployeeProfileMapping(profileRepository, Substitute.For<ILogger<CaseManagementItemEmployeeProfileMapping>>()),
            new CaseEditTaxonomyMapping(factory));
        return new CaseService(caseRepository, mapper, userProvider);
    }

    private static ISingleCaseRepository SetupCaseRepository(CaseEntity originalEntity)
    {
        var caseRepo = Substitute.For<ISingleCaseRepository>();
        caseRepo.GetCase(originalEntity.Id, originalEntity.Key).Returns(originalEntity);
        return caseRepo;
    }
}