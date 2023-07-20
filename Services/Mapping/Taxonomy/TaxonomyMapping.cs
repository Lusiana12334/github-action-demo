using AutoMapper;

namespace PEXC.Case.Services.Mapping.Taxonomy;

public abstract class TaxonomyMapping<TSource, TDestination> : IMappingAction<TSource, TDestination>
{
    private readonly ITaxonomyServiceFactory _taxonomyServiceFactory;

    protected TaxonomyMapping(ITaxonomyServiceFactory taxonomyServiceFactory)
    {
        _taxonomyServiceFactory = taxonomyServiceFactory;
    }

    public void Process(TSource source, TDestination destination, ResolutionContext context)
    {
        var taxonomyService = _taxonomyServiceFactory.Create().GetAwaiter().GetResult();
        MapTaxonomy(taxonomyService, source, destination, context);
    }

    protected abstract void MapTaxonomy(
        ITaxonomyService taxonomyService,
        TSource source,
        TDestination destination,
        ResolutionContext context);
}