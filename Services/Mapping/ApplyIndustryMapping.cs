using AutoMapper;
using Microsoft.Extensions.Options;
using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;
using PEXC.Case.Services.Infrastructure;

namespace PEXC.Case.Services.Mapping;

public class ApplyIndustryMapping : IMappingAction<CaseEntity, CaseRequestInfoDto>
{
    private readonly CaseSearchabilityOptions _options;

    public ApplyIndustryMapping(IOptions<CaseSearchabilityOptions> options)
    {
        _options = options.Value;
    }

    public void Process(CaseEntity source, CaseRequestInfoDto destination, ResolutionContext context)
    {
        destination.IsIndustryConfidential = source.PrimaryIndustry != null
                                             && _options.ConfidentialIndustries.Contains(source.PrimaryIndustry.Id.GetValueOrDefault());
        destination.PrimaryIndustry = source.PrimaryIndustry?.Name;
    }
}