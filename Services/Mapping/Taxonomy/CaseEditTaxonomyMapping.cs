using AutoMapper;
using PEXC.Case.DataContracts.V1;
using PEXC.Case.DataContracts.V1.CaseForms;
using PEXC.Case.Domain;

namespace PEXC.Case.Services.Mapping.Taxonomy;

public class CaseEditTaxonomyMapping : TaxonomyMapping<CaseEntity, CaseEditDto>
{
    public CaseEditTaxonomyMapping(ITaxonomyServiceFactory taxonomyServiceFactory) 
        : base(taxonomyServiceFactory)
    { }

    protected override void MapTaxonomy(
        ITaxonomyService taxonomyService,
        CaseEntity source,
        CaseEditDto destination,
        ResolutionContext context)
    {
        var caseDetails = destination.CaseDetailsSection!;
        var targetDetails = destination.TargetDetailsSection!.TargetDetailsAndFinalDocumentsSection!;

        caseDetails.ManagingOffice = context.Mapper.Map<TaxonomyOfficeDto>(taxonomyService.MapOfficeTaxonomy(source.ManagingOffice));
        caseDetails.PrimaryIndustry = context.Mapper.Map<TaxonomyItemDto>(taxonomyService.MapIndustryTaxonomy(source.PrimaryIndustry));
        caseDetails.PrimaryCapability = context.Mapper.Map<TaxonomyItemDto>(taxonomyService.MapCapabilityTaxonomy(source.PrimaryCapability));
        targetDetails.SecondaryIndustries = source.SecondaryIndustries
            ?.ConvertAll(item => context.Mapper.Map<TaxonomyItemDto>(taxonomyService.MapIndustryTaxonomy(item)));
        targetDetails.SecondaryIndustriesPaths = source.SecondaryIndustries
            ?.ConvertAll(
                item => new TaxonomyPathDto(
                    taxonomyService.MapIndustryTaxonomyPath(item)
                        .SkipWhile(s => s == null)
                        .Cast<string>()
                        .ToArray()));
    }
}