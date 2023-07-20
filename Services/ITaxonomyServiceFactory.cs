namespace PEXC.Case.Services;

public interface ITaxonomyServiceFactory
{
    Task<ITaxonomyService> Create();
}