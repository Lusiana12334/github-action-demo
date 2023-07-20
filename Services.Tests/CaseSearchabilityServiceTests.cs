using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PEXC.Case.DataAccess;
using PEXC.Case.Domain;
using PEXC.Case.Services.Infrastructure;
using PEXC.Common.BaseApi.User;

namespace PEXC.Case.Services.Tests;

public class CaseSearchabilityServiceTests
{
    [Fact]
    public async Task UpdateCasesSearchability_CasesAddedOrRemovedFromSearch_IsSearchableValueUpdated()
    {
        // Arrange
        var caseRepo = Substitute.For<ISingleCaseRepository>();
        var caseRepo2 = Substitute.For<ICaseRepository>();
        var caseReadyForSearch = new CaseEntity("1", "ABC123", RelationshipType.NonRetainer)
            { CorrelationId = Guid.NewGuid() };
        var caseRemovedFromSearch = new CaseEntity("2", "DEF456", RelationshipType.Retainer)
            { CorrelationId = Guid.NewGuid() };
        var caseAfterGracePeriod = new CaseEntity("3", "GHI789", RelationshipType.Retainer)
            { CorrelationId = Guid.NewGuid() };
        caseRepo2
            .GetCasesReadyForSearch(Arg.Any<TimeSpan>(), Arg.Any<int?>(), Arg.Any<string?>())
            .Returns(new PagedResult<CaseEntity> { Items = new[] { caseReadyForSearch } });
        caseRepo2
            .GetCasesRemovedFromSearch(Arg.Any<int?>(), Arg.Any<string?>())
            .Returns(new PagedResult<CaseEntity> { Items = new[] { caseRemovedFromSearch } });
        caseRepo2
            .GetCasesAfterConfidentialGracePeriod(Arg.Any<TimeSpan>(), Arg.Any<int?>(), Arg.Any<string?>())
            .Returns(new PagedResult<CaseEntity> { Items = new[] { caseAfterGracePeriod } });

        var service = CreateService(caseRepo2, caseRepo);

        // Act
        await service.UpdateCasesSearchability();

        // Assert
        await caseRepo
            .Received()
            .PatchCase(
                caseReadyForSearch.Id,
                caseReadyForSearch.Key,
                Arg.Is<IReadOnlyDictionary<string, object?>>(
                    d => d.Contains(new KeyValuePair<string, object?>(nameof(CaseEntity.IsSearchable).ToCamelCase(), true)) &&
                         d.Contains(new KeyValuePair<string, object?>(nameof(CaseEntity.CorrelationId).ToCamelCase(), caseReadyForSearch.CorrelationId)) &&
                         d.ContainsKey(nameof(CaseEntity.ModifiedBy).ToCamelCase()) &&
                         d.ContainsKey(nameof(CaseEntity.Modified).ToCamelCase())));
        await caseRepo
            .Received()
            .PatchCase(
                caseRemovedFromSearch.Id,
                caseRemovedFromSearch.Key,
                Arg.Is<IReadOnlyDictionary<string, object?>>(
                    d => d.Contains(new KeyValuePair<string, object?>(nameof(CaseEntity.IsSearchable).ToCamelCase(), false)) &&
                         d.Contains(new KeyValuePair<string, object?>(nameof(CaseEntity.CorrelationId).ToCamelCase(), caseRemovedFromSearch.CorrelationId)) &&
                         d.ContainsKey(nameof(CaseEntity.ModifiedBy).ToCamelCase()) &&
                         d.ContainsKey(nameof(CaseEntity.Modified).ToCamelCase())));
        await caseRepo
            .Received()
            .PatchCase(
                caseAfterGracePeriod.Id,
                caseAfterGracePeriod.Key,
                Arg.Is<IReadOnlyDictionary<string, object?>>(
                    d => d.Contains(new KeyValuePair<string, object?>(nameof(CaseEntity.IsInConfidentialGracePeriod).ToCamelCase(), false)) &&
                         d.Contains(new KeyValuePair<string, object?>(nameof(CaseEntity.CorrelationId).ToCamelCase(), caseAfterGracePeriod.CorrelationId)) &&
                         d.ContainsKey(nameof(CaseEntity.ModifiedBy).ToCamelCase()) &&
                         d.ContainsKey(nameof(CaseEntity.Modified).ToCamelCase())));
    }

    private CaseSearchabilityService CreateService(ICaseRepository caseRepo, ISingleCaseRepository caseRepository, IUserProvider? userProvider = null)
    {
        var factory = Substitute.For<ITaxonomyServiceFactory>();
        factory
            .Create()
            .Returns(Substitute.For<ITaxonomyService>());

        return new CaseSearchabilityService(
            Options.Create(new CaseSearchabilityOptions()),
            Substitute.For<ILogger<CaseSearchabilityService>>(),
            caseRepo, 
            caseRepository);
    }

}