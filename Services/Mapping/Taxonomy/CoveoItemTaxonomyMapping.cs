using AutoMapper;
using Microsoft.Extensions.Options;
using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;
using PEXC.Case.Services.Infrastructure;

namespace PEXC.Case.Services.Mapping.Taxonomy;

public class CoveoItemTaxonomyMapping<TDestination> : 
    TaxonomyMapping<CaseEntity, TDestination> where TDestination : CaseSearchItemDto
{
    private readonly CoveoMappingOptions _options;

    public CoveoItemTaxonomyMapping(ITaxonomyServiceFactory taxonomyServiceFactory, IOptions<CoveoMappingOptions> options) 
        : base(taxonomyServiceFactory)
    {
        _options = options.Value;
    }

    protected override void MapTaxonomy(
        ITaxonomyService taxonomyService,
        CaseEntity source,
        TDestination destination,
        ResolutionContext context)
    {
        destination.ClientType = taxonomyService.MapIndustryTaxonomy(source.PrimaryIndustry)?.Name;
        destination.CaseType = taxonomyService.MapCapabilityTaxonomy(source.PrimaryCapability)?.Name;

        if (source.PrimaryCapability is { Id: { } })
        {
            var caseTypeClusteredCapability =
                _options.CaseTypeClusteredCapability.FirstOrDefault(item => item.Value.Contains(source.PrimaryCapability.Id.Value));
            destination.CaseTypeClustered = caseTypeClusteredCapability.Key;
        }

        var office = taxonomyService.MapOfficeTaxonomy(source.ManagingOffice);
        destination.ManagingOffice = office?.Name;
        destination.Region = office?.Region;
        destination.OfficeCluster = office?.OfficeCluster;
        var industryPaths = (source.SecondaryIndustries ?? Enumerable.Empty<TaxonomyItem>())
            .Select(taxonomyService.MapIndustryTaxonomyPath)
            .ToArray();
        destination.TopLevelIndustry = JoinToString(industryPaths.Select(p => p[0]));
        destination.SecondLevelIndustry = JoinToString(industryPaths.Select(p => p.ElementAtOrDefault(1)));
        destination.PrimaryIndustry = JoinToString(industryPaths.SelectMany(p => p.Skip(2)));
    }

    private static string? JoinToString(IEnumerable<string?> items)
    {
        var result = string.Join(MainProfile.CoveoFieldSeparator, 
            items.Where(i => !string.IsNullOrEmpty(i)).Distinct());
        return string.IsNullOrEmpty(result) ? null : result;
    }
}