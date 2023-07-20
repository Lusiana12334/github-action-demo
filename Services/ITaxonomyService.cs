using PEXC.Case.Domain;

namespace PEXC.Case.Services;

public interface ITaxonomyService
{
    TaxonomyItem MapCapabilityTagId(string? tagId);
    TaxonomyItem MapIndustryTagId(string? tagId);
    TaxonomyItem? MapIndustryTaxonomy(TaxonomyItem? taxonomyItem);
    TaxonomyItem? MapIndustryTaxonomy(int? taxonomyId);
    TaxonomyItem? MapCapabilityTaxonomy(TaxonomyItem? taxonomyItem);
    TaxonomyItem? MapCapabilityTaxonomy(int? taxonomyId);
    TaxonomyOffice? MapOfficeTaxonomy(TaxonomyOffice? taxonomyOffice);
    TaxonomyOffice? MapOfficeTaxonomy(int? officeCode);
    IReadOnlyList<string?> MapIndustryTaxonomyPath(TaxonomyItem taxonomyItem);
}