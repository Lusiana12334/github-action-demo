using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PEXC.Case.DataAccess;
using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;
using PEXC.Case.Services.Infrastructure;
using PEXC.Case.Services.Mapping;
using PEXC.Case.Services.Mapping.EmployeeProfile;
using PEXC.Case.Services.Mapping.FieldMasking;
using PEXC.Case.Services.Mapping.Taxonomy;
using PEXC.Case.Tests.Common;
using PEXC.Common.BaseApi.Profile;

namespace PEXC.Case.Services.Tests;

public class CoveoCaseServiceTest : MappingAwareTestsBase<MainProfile>
{
    private readonly PaginationRequestDto _pagination = new(DateTime.UtcNow, 2, "ABC123");

    [Fact]
    public async Task GetSearchableCases_WhenCasesAvailable_ReturnsMappedCasesFromRepo()
    {
        // Arrange
        var entities = new PagedResult<CaseEntity>
        {
            NextPageToken = "DEF456",
            Items = new List<CaseEntity> { Fake.CaseEntity("1"), Fake.CaseEntity("2") }
        };

        var caseRepo = Substitute.For<ICaseRepository>();
        caseRepo
            .GetSearchableCases(_pagination.ModifiedSince, _pagination.PageSize!.Value, _pagination.NextPageToken!)
            .Returns(entities);

        var service = CreateService(caseRepo);

        // Act
        var result = await service.GetSearchableCases(_pagination);

        // Assert
        result.Items.Count
            .Should()
            .Be(entities.Items.Count);
        result.NextPageToken
            .Should()
            .Be(entities.NextPageToken);
    }

    [Theory]
    [InlineData(false, null, false)]
    [InlineData(false, false, false)]
    [InlineData(false, true, true)]
    [InlineData(true, null, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    public async Task GetSearchableCases_WhenCasesInDifferentStates_ReturnsMappedCasesWithCorrectSearchableState(
        bool isDeleted, bool? isSearchable, bool expectedSearchable)
    {
        // Arrange
        var caseEntity = Fake.CaseEntity("3");
        caseEntity.PrimaryIndustry = null;
        caseEntity.PrimaryCapability = null;
        caseEntity.ItemStage = isDeleted ? CaseState.Deleted : caseEntity.ItemStage;
        caseEntity.IsSearchable = isSearchable;
        var entities = new PagedResult<CaseEntity>
        {
            NextPageToken = "DEF456",
            Items = new List<CaseEntity> { caseEntity }
        };

        var caseRepo = Substitute.For<ICaseRepository>();
        caseRepo
            .GetSearchableCases(_pagination.ModifiedSince, _pagination.PageSize!.Value, _pagination.NextPageToken!)
            .Returns(entities);

        var service = CreateService(caseRepo);

        // Act
        var result = await service.GetSearchableCases(_pagination);

        // Assert
        result.Items[0].IsNotSearchable.Should().Be(!expectedSearchable);
    }

    [Fact]
    public async Task GetSearchableCases_WhenIndustryIsOnTheClusteredList_ReturnsMappedCasesWithCorrectClientTypeClustered()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity("3");
        caseEntity.PrimaryCapability = new TaxonomyItem(10, "AA");
        var entities = new PagedResult<CaseEntity>
        {
            NextPageToken = "DEF456",
            Items = new List<CaseEntity> { caseEntity }
        };

        var caseRepo = Substitute.For<ICaseRepository>();
        caseRepo
            .GetSearchableCases(_pagination.ModifiedSince, _pagination.PageSize!.Value, _pagination.NextPageToken!)
            .Returns(entities);

        var service = CreateService(caseRepo,
            new CoveoMappingOptions()
            {
                CaseTypeClusteredCapability =  new Dictionary<string, IEnumerable<int>>() { { nameof(CaseSearchItemDto.CaseTypeClustered), new [] { 1,2, 10, 100} } }
            });

        // Act
        var result = await service.GetSearchableCases(_pagination);

        result.Items.First().CaseTypeClustered.Should().Be(nameof(CaseSearchItemDto.CaseTypeClustered));
    }

    [Fact]
    public async Task GetActiveCases_WhenIndustryIsOnTheClusteredList_ReturnsMappedCasesWithCorrectClientTypeClustered()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity("3");
        caseEntity.PrimaryCapability = new TaxonomyItem(10, "AA");
        var entities = new PagedResult<CaseEntity>
        {
            NextPageToken = "DEF456",
            Items = new List<CaseEntity> { caseEntity }
        };

        var caseRepo = Substitute.For<ICaseRepository>();
        caseRepo
            .GetActiveCases(_pagination.ModifiedSince, _pagination.PageSize!.Value, _pagination.NextPageToken!)
            .Returns(entities);

        var service = CreateService(caseRepo,
            new CoveoMappingOptions()
            {
                CaseTypeClusteredCapability = new Dictionary<string, IEnumerable<int>>() { { nameof(CaseSearchItemDto.CaseTypeClustered), new[] { 1, 2, 10, 100 } } }
            });

        // Act
        var result = await service.GetActiveCases(_pagination);

        result.Items.First().CaseTypeClustered.Should().Be(nameof(CaseSearchItemDto.CaseTypeClustered));
    }


    [Fact]
    public async Task GetActiveCases_WhenCasesAvailable_ReturnsMappedCasesFromRepo()
    {
        var test = new CaseEntity("3", "3333", RelationshipType.Retainer);
        // Arrange
        var entities = new PagedResult<CaseEntity>
        {
            NextPageToken = "DEF456",
            Items = new List<CaseEntity> { Fake.CaseEntity("1"), Fake.CaseEntity("2"), test }
        };

        var caseRepo = Substitute.For<ICaseRepository>();
        caseRepo
            .GetActiveCases(_pagination.ModifiedSince, _pagination.PageSize!.Value, _pagination.NextPageToken!)
            .Returns(entities);

        var service = CreateService(caseRepo);

        // Act
        var result = await service.GetActiveCases(_pagination);

        // Assert
        result.Items.Count
            .Should()
            .Be(entities.Items.Count);
        result.NextPageToken
            .Should()
            .Be(entities.NextPageToken);
    }


    private CoveoCaseService CreateService(ICaseRepository caseRepository, CoveoMappingOptions? coveoMappingOptions = null)
    {
        var factory = Substitute.For<ITaxonomyServiceFactory>();
        factory
            .Create()
            .Returns(Substitute.For<ITaxonomyService>());

        var profileRepository = Substitute.For<IProfileRepository>();
        var mapper = CreateMapper(
            new CoveoItemTaxonomyMapping<CaseSearchItemDto>(factory, Options.Create(coveoMappingOptions ?? new CoveoMappingOptions())),
            new CoveoItemEmployeeProfileMapping<CaseSearchItemDto>(profileRepository, Substitute.For<ILogger<CoveoItemEmployeeProfileMapping<CaseSearchItemDto>>>()),
            new CaseSearchItemConfidentialDataMapping(Substitute.For<IFieldMaskingPolicy>()),
            new CoveoItemTaxonomyMapping<CaseManagementItemDto>(factory, Options.Create(coveoMappingOptions ?? new CoveoMappingOptions())),
            new CaseManagementItemEmployeeProfileMapping(profileRepository, Substitute.For<ILogger<CaseManagementItemEmployeeProfileMapping>>()),
            new CaseEditTaxonomyMapping(factory));

        return new CoveoCaseService(caseRepository, mapper, Substitute.For<ILogger<CoveoCaseService>>());
    }

}