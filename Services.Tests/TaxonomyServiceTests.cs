using PEXC.Case.Domain;
using PEXC.Case.Services.Mapping;
using PEXC.Case.Tests.Common;
using PEXC.Common.Taxonomy.DataContracts;

namespace PEXC.Case.Services.Tests;

public class TaxonomyServiceTests : MappingAwareTestsBase<MainProfile>
{
    private readonly TermDto _newYorkOffice = new()
    {
        Id = 555,
        OfficeCode = 110,
        Name = "New York",
        Parent = new TermDto
        {
            Id = 777,
            OfficeCode = 4,
            Name = "AMER",
        }
    };

    private readonly TermDto _capabilityTerm = new() { Id = 123, TagId = Guid.NewGuid(), Name = "Cost Opt." };

    private readonly TermDto _industryTerm = new() { Id = 456, TagId = Guid.NewGuid(), Name = "Air transport" };

    private readonly TermDto _industryTerm2 = new() { Id = 201, TagId = Guid.NewGuid(), Name = "See transportation" };

    private readonly TermDto _indTreeTerm = new()
    {
        Id = 10,
        Name = "Civil Aircrafts",
        Parent = new TermDto
        {
            Id = 20,
            TagId = Guid.NewGuid(),
            Name = "Aircrafts",
            Parent = new TermDto
            {
                Id = 30,
                Name = "Air transport & infrastructure",
                Parent = new TermDto
                {
                    Id = 40,
                    Name = "Transport",
                },
            },
        }
    };

    [Fact]
    public void MapCapabilityTagId()
    {
        // Arrange
        var sut = CreateTaxonomyService();

        // Act
        var invalidGuid = sut.MapCapabilityTagId("invalid guid");
        var missingEntry = sut.MapCapabilityTagId(Guid.NewGuid().ToString());
        var correct = sut.MapCapabilityTagId(_capabilityTerm.TagId.ToString());

        // Assert
        invalidGuid.Should().Be(TaxonomyItem.Empty);
        missingEntry.Should().Be(TaxonomyItem.Empty);
        correct.Should().Be(new TaxonomyItem(_capabilityTerm.Id, _capabilityTerm.Name));
    }

    [Fact]
    public void MapIndustryTagId()
    {
        // Arrange
        var sut = CreateTaxonomyService();

        // Act
        var invalidGuid = sut.MapIndustryTagId("invalid guid");
        var missingEntry = sut.MapIndustryTagId(Guid.NewGuid().ToString());
        var correct = sut.MapIndustryTagId(_industryTerm.TagId.ToString());

        // Assert
        invalidGuid.Should().Be(TaxonomyItem.Empty);
        missingEntry.Should().Be(TaxonomyItem.Empty);
        correct.Should().Be(new TaxonomyItem(_industryTerm.Id, _industryTerm.Name));
    }

    [Fact]
    public void MapIndustryTaxonomy_MatchingTermExists_ReturnsMappedTaxonomy()
    {
        // Arrange
        var sut = CreateTaxonomyService();

        // Act
        var result = sut.MapIndustryTaxonomy(new TaxonomyItem(_industryTerm.Id, null))!;

        // Assert
        result.Id
            .Should()
            .Be(_industryTerm.Id);
        result.Name
            .Should()
            .Be(_industryTerm.Name);
    }

    [Fact]
    public void MapIndustryTaxonomy_MatchingTermDoesNotExist_ReturnsInputTaxonomy()
    {
        // Arrange
        var sut = CreateTaxonomyService();

        // Act
        var taxonomyItem = new TaxonomyItem(12345, null);
        var result = sut.MapIndustryTaxonomy(taxonomyItem);

        // Assert
        result
            .Should()
            .BeSameAs(taxonomyItem);
    }

    [Fact]
    public void MapIndustryTaxonomy_InputIdIsNull_ReturnsInputTaxonomy()
    {
        // Arrange
        var sut = CreateTaxonomyService();

        // Act
        var taxonomyItem = new TaxonomyItem(null, null);
        var result = sut.MapIndustryTaxonomy(taxonomyItem);

        // Assert
        result
            .Should()
            .BeSameAs(taxonomyItem);
    }

    [Fact]
    public void MapIndustryTaxonomy_InputIsNull_ReturnsNull()
    {
        // Arrange
        var sut = CreateTaxonomyService();

        // Act
        var result = sut.MapIndustryTaxonomy((TaxonomyItem?)null);

        // Assert
        result
            .Should()
            .BeNull();
    }

    [Fact]
    public void MapIndustryTaxonomyById_MatchingTermExists_ReturnsMappedTaxonomy()
    {
        // Arrange
        var sut = CreateTaxonomyService();

        // Act
        var result = sut.MapIndustryTaxonomy(_industryTerm.Id)!;

        // Assert
        result.Id
            .Should()
            .Be(_industryTerm.Id);
        result.Name
            .Should()
            .Be(_industryTerm.Name);
    }

    [Fact]
    public void MapIndustryTaxonomyById_MatchingTermDoesNotExist_ReturnsNull()
    {
        // Arrange
        var sut = CreateTaxonomyService();

        // Act
        var result = sut.MapIndustryTaxonomy(12345);

        // Assert
        result
            .Should()
            .BeNull();
    }

    [Fact]
    public void MapIndustryTaxonomyById_InputIdIsNull_ReturnsNull()
    {
        // Arrange
        var sut = CreateTaxonomyService();

        // Act
        var result = sut.MapIndustryTaxonomy((int?)null);

        // Assert
        result
            .Should()
            .BeNull();
    }

    [Fact]
    public void MapCapabilityTaxonomy_MatchingTermExists_ReturnsMappedTaxonomy()
    {
        // Arrange
        var sut = CreateTaxonomyService();

        // Act
        var result = sut.MapCapabilityTaxonomy(new TaxonomyItem(_capabilityTerm.Id, null))!;

        // Assert
        result.Id
            .Should()
            .Be(_capabilityTerm.Id);
        result.Name
            .Should()
            .Be(_capabilityTerm.Name);
    }

    [Fact]
    public void MapCapabilityTaxonomy_MatchingTermDoesNotExist_ReturnsInputTaxonomy()
    {
        // Arrange
        var sut = CreateTaxonomyService();

        // Act
        var taxonomyItem = new TaxonomyItem(12345, null);
        var result = sut.MapCapabilityTaxonomy(taxonomyItem);

        // Assert
        result
            .Should()
            .BeSameAs(taxonomyItem);
    }

    [Fact]
    public void MapCapabilityTaxonomy_InputIdIsNull_ReturnsInputTaxonomy()
    {
        // Arrange
        var sut = CreateTaxonomyService();

        // Act
        var taxonomyItem = new TaxonomyItem(null, null);
        var result = sut.MapCapabilityTaxonomy(taxonomyItem);

        // Assert
        result
            .Should()
            .BeSameAs(taxonomyItem);
    }

    [Fact]
    public void MapCapabilityTaxonomy_InputIsNull_ReturnsNull()
    {
        // Arrange
        var sut = CreateTaxonomyService();

        // Act
        var result = sut.MapCapabilityTaxonomy((TaxonomyItem?)null);

        // Assert
        result
            .Should()
            .BeNull();
    }

    [Fact]
    public void MapCapabilityTaxonomyById_MatchingTermExists_ReturnsMappedTaxonomy()
    {
        // Arrange
        var sut = CreateTaxonomyService();

        // Act
        var result = sut.MapCapabilityTaxonomy(_capabilityTerm.Id)!;

        // Assert
        result.Id
            .Should()
            .Be(_capabilityTerm.Id);
        result.Name
            .Should()
            .Be(_capabilityTerm.Name);
    }

    [Fact]
    public void MapCapabilityTaxonomyById_MatchingTermDoesNotExist_ReturnsNull()
    {
        // Arrange
        var sut = CreateTaxonomyService();

        // Act
        var result = sut.MapCapabilityTaxonomy(12345);

        // Assert
        result
            .Should()
            .BeNull();
    }

    [Fact]
    public void MapCapabilityTaxonomyById_InputIdIsNull_ReturnsNull()
    {
        // Arrange
        var sut = CreateTaxonomyService();

        // Act
        var result = sut.MapCapabilityTaxonomy((int?)null);

        // Assert
        result
            .Should()
            .BeNull();
    }

    [Fact]
    public void MapOfficeTaxonomy_MatchingOfficeExists_ReturnsMappedOffice()
    {
        // Arrange
        var sut = CreateTaxonomyService();

        // Act
        var result = sut.MapOfficeTaxonomy(new TaxonomyOffice(_newYorkOffice.OfficeCode, "SomeName", "Cluster", null))!;

        // Assert
        result.Code
            .Should()
            .Be(_newYorkOffice.OfficeCode);
        result.Name
            .Should()
            .Be(_newYorkOffice.Name);
        result.Region
            .Should()
            .Be(_newYorkOffice.Parent.Name);
    }

    [Fact]
    public void MapOfficeTaxonomy_MatchingOfficeDoesNotExist_ReturnsInputOffice()
    {
        // Arrange
        var sut = CreateTaxonomyService();

        // Act
        var taxonomyOffice = new TaxonomyOffice(12345, "SomeName", "Cluster", null);
        var result = sut.MapOfficeTaxonomy(taxonomyOffice);

        // Assert
        result
            .Should()
            .BeSameAs(taxonomyOffice);
    }

    [Fact]
    public void MapOfficeTaxonomy_OfficeCodeIsNull_ReturnsInputTaxonomy()
    {
        // Arrange
        var sut = CreateTaxonomyService();

        // Act
        var taxonomyOffice = new TaxonomyOffice(null, "SomeName", "Cluster", null);
        var result = sut.MapOfficeTaxonomy(taxonomyOffice);

        // Assert
        result
            .Should()
            .BeSameAs(taxonomyOffice);
    }

    [Fact]
    public void MapOfficeTaxonomy_InputIsNull_ReturnsNull()
    {
        // Arrange
        var sut = CreateTaxonomyService();

        // Act
        var result = sut.MapOfficeTaxonomy((TaxonomyOffice?)null);

        // Assert
        result
            .Should()
            .BeNull();
    }

    [Fact]
    public void MapIndustryTaxonomyPath_MatchingTermExists_ReturnsMappedPath()
    {
        // Arrange
        var sut = CreateTaxonomyService();

        // Act
        var result = sut.MapIndustryTaxonomyPath(new TaxonomyItem(_indTreeTerm.Id, "SomeName"));

        // Assert
        result
            .Should()
            .BeEquivalentTo(
                _indTreeTerm.Parent.Parent.Parent.Name,
                _indTreeTerm.Parent.Parent.Name,
                _indTreeTerm.Parent.Name,
                _indTreeTerm.Name);
    }

    [Fact]
    public void MapIndustryTaxonomyPath_MatchingTermDoesNotExist_ReturnsNullsForTop2Entries()
    {
        // Arrange
        var sut = CreateTaxonomyService();

        // Act
        var taxonomyItem = new TaxonomyItem(12345, "SomeName");
        var result = sut.MapIndustryTaxonomyPath(taxonomyItem);

        // Assert
        result
            .Should()
            .BeEquivalentTo(
                (string?)null,
                null,
                taxonomyItem.Name);
    }

    [Fact]
    public void MapIndustryTaxonomyPath_InputIdIsNull_ReturnsNullsForTop2Entries()
    {
        // Arrange
        var sut = CreateTaxonomyService();

        // Act
        var taxonomyItem = new TaxonomyItem(null, "SomeName");
        var result = sut.MapIndustryTaxonomyPath(taxonomyItem);

        // Assert
        result
            .Should()
            .BeEquivalentTo(
                (string?)null,
                null,
                taxonomyItem.Name);
    }

    [Fact]
    public void MapOfficeTaxonomy_MatchingOfficeCodeExists_ReturnsMappedOffice()
    {
        // Arrange
        var sut = CreateTaxonomyService();

        // Act
        var result = sut.MapOfficeTaxonomy(_newYorkOffice.OfficeCode)!;

        // Assert
        result.Code
            .Should()
            .Be(_newYorkOffice.OfficeCode);
        result.Name
            .Should()
            .Be(_newYorkOffice.Name);
        result.Region
            .Should()
            .Be(_newYorkOffice.Parent.Name);
    }

    [Fact]
    public void MapOfficeTaxonomy_MatchingOfficeCodeDoesNotExist_ReturnsNull()
    {
        // Arrange
        var sut = CreateTaxonomyService();

        // Act
        var result = sut.MapOfficeTaxonomy(123123);

        // Assert
        result
            .Should()
            .BeNull();
    }

    private TaxonomyService CreateTaxonomyService()
    {
        var offices = new Dictionary<int, TermDto>
        {
            { _newYorkOffice.OfficeCode, _newYorkOffice },
        };
        var industries = new Dictionary<int, TermDto>
        {
            { _industryTerm.Id, _industryTerm },
            { _indTreeTerm.Id, _indTreeTerm },
            { _industryTerm2.Id, _industryTerm2 },
        };
        var capabilities = new Dictionary<int, TermDto>
        {
            { _capabilityTerm.Id, _capabilityTerm },
        };

        return new TaxonomyService(offices, industries, capabilities);
    }
}