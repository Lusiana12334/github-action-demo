using System.Linq.Expressions;
using System.Reflection;
using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;
using PEXC.Case.Services.Mapping;
using PEXC.Case.Services.Mapping.FieldMasking;
using PEXC.Case.Tests.Common;

namespace PEXC.Case.Services.Tests.Mapping;

public class FieldMaskingTests
{
    private const string ConfidentialDataPlaceholder = "Legally cannot be disclosed";

    private static readonly Expression<Func<CaseSearchItemDto, string?>>[] MaskedProperties = {
        destination => destination.ClientName,
        destination => destination.TargetName,
        destination => destination.TargetDescription,
        destination => destination.MainCompetitorsAnalyzed,
        destination => destination.FinalMaterialAvailable,
        destination => destination.ManagerName,
        destination => destination.Keyword,
        destination => destination.IndustrySectorsAnalyzed,
        destination => destination.GeographicRegion,
        destination => destination.AdvancedAnalyticsUsage,
        destination => destination.OpsDdComponent,
        destination => destination.OpsDdDuration,
        destination => destination.OpsDdTeam,
    };

    private static readonly Func<CaseSearchItemDto, string?>[] PropertyAccessors =
        Array.ConvertAll(MaskedProperties, exp => exp.Compile());

    [Fact]
    public void CompositeFieldMaskingPolicy_CallsAllItsChildren()
    {
        // Arrange
        var first = Substitute.For<IFieldMaskingPolicy>();
        var second = Substitute.For<IFieldMaskingPolicy>();
        var innerItems = new List<IFieldMaskingPolicy> { first, second };
        var composite = new CompositeFieldMaskingPolicy(innerItems);

        // Act
        composite.Apply(null!, null!);

        // Assert
        first.Received().Apply(null!, null!);
        second.Received().Apply(null!, null!);
    }

    [Fact]
    public void WhenMappingSearchData_AutomapperCallsFieldMasking()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        var searchItem = Fake.CaseSearchItemDto(caseEntity.Id);
        var fieldMask = Substitute.For<IFieldMaskingPolicy>();
        var mapping = new CaseSearchItemConfidentialDataMapping(fieldMask);

        // Act
        mapping.Process(caseEntity, searchItem, null!);

        // Assert
        fieldMask.Received().Apply(caseEntity, searchItem);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    public void WhenCaseInConfidentialGracePeriod_SomeDataMaskedAsConfidential(bool? isInGracePeriod)
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        var searchItem = Fake.CaseSearchItemDto(caseEntity.Id);
        searchItem.ClientName = caseEntity.ClientName;
        searchItem.TargetName = caseEntity.TargetName;
        caseEntity.IsInConfidentialGracePeriod = isInGracePeriod;

        // Act
        var fieldMaskPolicy = new ConfidentialGracePeriodFieldMaskingPolicy(ConfidentialDataPlaceholder);
        fieldMaskPolicy.Apply(caseEntity, searchItem);

        // Assert
        searchItem.ClientName.Should().Be(caseEntity.ClientName);
        searchItem.TargetName.Should().Be(ConfidentialDataPlaceholder);
    }

    [Fact]
    public void WhenConfidentialClient_SomeDataMaskedAsConfidential()
    {
        // Arrange
        const int confidentialIndustry = 99999;
        var caseEntity = Fake.CaseEntity();
        var searchItemDto = CreateCaseSearchItemDto(caseEntity);

        caseEntity.PrimaryIndustry = new TaxonomyItem(confidentialIndustry, "Ind1234");
        var fieldMasking =
            new HedgeFundCasesFieldMaskingPolicy(ConfidentialDataPlaceholder, new [] { confidentialIndustry });

        // Act
        fieldMasking.Apply(caseEntity, searchItemDto);

        // Assert
        foreach (var propertyAccessor in PropertyAccessors)
            propertyAccessor(searchItemDto).Should().Be(ConfidentialDataPlaceholder);
    }
    
    [Fact]
    public void WhenTargetPubliclyTraded_SomeDataMaskedAsConfidential()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        caseEntity.TargetPubliclyTraded = true;
        var searchItemDto = CreateCaseSearchItemDto(caseEntity);

        var fieldMasking =
            new TargetPubliclyTradedFieldMaskingPolicy(ConfidentialDataPlaceholder);

        // Act
        fieldMasking.Apply(caseEntity, searchItemDto);

        // Assert
        foreach (var propertyAccessor in PropertyAccessors)
            propertyAccessor(searchItemDto).Should().Be(ConfidentialDataPlaceholder);
    }

    [Fact]
    public void WhenSensitive_SomeDataMaskedAsConfidential()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        caseEntity.Sensitive = true;
        var searchItemDto = CreateCaseSearchItemDto(caseEntity);

        var fieldMasking =
            new SensitiveFieldMaskingPolicy(ConfidentialDataPlaceholder);

        // Act
        fieldMasking.Apply(caseEntity, searchItemDto);

        // Assert
        foreach (var propertyAccessor in PropertyAccessors)
            propertyAccessor(searchItemDto).Should().Be(ConfidentialDataPlaceholder);
    }

    private static CaseSearchItemDto CreateCaseSearchItemDto(CaseEntity caseEntity)
    {
        var searchItemDto = Fake.CaseSearchItemDto(caseEntity.Id);

        foreach (var propertyAccessor in MaskedProperties)
        {
            var propertyInfo = (PropertyInfo)((MemberExpression)propertyAccessor.Body).Member;
            propertyInfo.SetValue(searchItemDto, Guid.NewGuid().ToString());
        }

        return searchItemDto;
    }
}