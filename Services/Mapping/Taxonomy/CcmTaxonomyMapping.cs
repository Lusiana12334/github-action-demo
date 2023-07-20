using AutoMapper;
using Microsoft.Extensions.Logging;
using PEXC.Case.Domain;
using PEXC.Case.Services.CCM.Contracts;

namespace PEXC.Case.Services.Mapping.Taxonomy;

public class CcmTaxonomyMapping : TaxonomyMapping<CaseDetailsDto, CaseEntity>
{
    private readonly ILogger<CcmTaxonomyMapping> _logger;

    public CcmTaxonomyMapping(
        ITaxonomyServiceFactory taxonomyServiceFactory,
        ILogger<CcmTaxonomyMapping> logger)
        : base(taxonomyServiceFactory)
    {
        _logger = logger;
    }

    protected override void MapTaxonomy(
        ITaxonomyService taxonomyService,
        CaseDetailsDto source,
        CaseEntity destination,
        ResolutionContext context)
    {
        destination.PrimaryIndustry = MapIndustry(taxonomyService, destination, source.PrimaryIndustryTagId);
        destination.PrimaryCapability = MapCapability(taxonomyService, destination, source.PrimaryCapabilityTagId);
        destination.SecondaryIndustries = source.SecondaryIndustry
            .Select(c => MapIndustry(taxonomyService, destination, c.TagId))
            .ToList();
        destination.SecondaryCapabilities = source.SecondaryCapability
            .Select(c => MapCapability(taxonomyService, destination, c.TagId))
            .ToList();
        destination.ManagingOffice = taxonomyService.MapOfficeTaxonomy(source.CaseOffice);
    }

    private TaxonomyItem MapIndustry(ITaxonomyService service, CaseEntity entity, string? tagId)
        => LogIfTaxonomyIsMissing(entity, tagId, service.MapIndustryTagId(tagId));

    private TaxonomyItem MapCapability(ITaxonomyService service, CaseEntity entity, string? tagId)
        => LogIfTaxonomyIsMissing(entity, tagId, service.MapCapabilityTagId(tagId));

    private TaxonomyItem LogIfTaxonomyIsMissing(CaseEntity entity, string? tagId, TaxonomyItem item)
    {
        if (item.Id == null)
        {
            _logger.LogWarning("Cannot map tag Id: {taxonomyTagId} to PoolParty id for case: {caseId} {caseCode}",
                tagId, entity.Id, entity.CaseCode);
        }

        return item;
    }
}