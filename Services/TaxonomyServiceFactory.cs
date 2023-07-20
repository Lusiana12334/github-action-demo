using PEXC.Common.Taxonomy;
using PEXC.Common.Taxonomy.DataContracts;

namespace PEXC.Case.Services;

public class TaxonomyServiceFactory : ITaxonomyServiceFactory
{
    private readonly ITaxonomyRepository _taxonomyRepository;

    public TaxonomyServiceFactory(ITaxonomyRepository taxonomyRepository)
    {
        _taxonomyRepository = taxonomyRepository;
    }

    public async Task<ITaxonomyService> Create()
    {
        var offices = (await _taxonomyRepository.GetFlatTaxonomy(TaxonomyType.Office))
            .Values
            .Where(o => o.OfficeCode != default)
            .ToDictionary(p => p.OfficeCode);
        var industries = await _taxonomyRepository.GetFlatTaxonomy(TaxonomyType.Industry);
        var capabilities = await _taxonomyRepository.GetFlatTaxonomy(TaxonomyType.Capability);

        return new TaxonomyService(offices, industries, capabilities);
    }
}
    