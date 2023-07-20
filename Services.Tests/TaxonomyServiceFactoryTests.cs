using PEXC.Common.Taxonomy;
using PEXC.Common.Taxonomy.DataContracts;

namespace PEXC.Case.Services.Tests;

public class TaxonomyServiceFactoryTests
{
    [Fact]
    public async Task BuildUsesTaxonomyRepositoryResults()
    {
        var boston = new TermDto { Id = 15, OfficeCode = 110, Name = "Boston" };
        var repository = Substitute.For<ITaxonomyRepository>();
        var industries = new Dictionary<int, TermDto>();
        var capabilites = new Dictionary<int, TermDto>();
        var offices = new Dictionary<int, TermDto>
        {
            {15,  boston}
        };

        repository.GetFlatTaxonomy(TaxonomyType.Industry).Returns(industries);
        repository.GetFlatTaxonomy(TaxonomyType.Capability).Returns(capabilites);
        repository.GetFlatTaxonomy(TaxonomyType.Office).Returns(offices);

        var factory = new TaxonomyServiceFactory(repository);

        var service = (TaxonomyService)await factory.Create();
        service.Industries.Should().BeSameAs(industries);
        service.Capabilities.Should().BeSameAs(capabilites);
        service.Offices.Should().BeEquivalentTo(
            new Dictionary<int, TermDto>
            {
                {110,  boston}
            });
    }
}
