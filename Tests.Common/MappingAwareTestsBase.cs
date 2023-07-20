using AutoMapper;

namespace PEXC.Case.Tests.Common;

public abstract class MappingAwareTestsBase<TProfile>
    where TProfile : Profile, new()
{
    protected IMapper Mapper { get; init; }

    protected MappingAwareTestsBase()
    {
        Mapper = CreateMapper();
    }

    protected IMapper CreateMapper(params object[] dependencies)
    {
        var config = new MapperConfiguration(
            cfg =>
            {
                var depDictionary = dependencies.ToDictionary(d => d.GetType());
                cfg.AddProfile<TProfile>();
                cfg.ConstructServicesUsing(type => depDictionary.GetValueOrDefault(type));
            });
        return config.CreateMapper();
    }
}
