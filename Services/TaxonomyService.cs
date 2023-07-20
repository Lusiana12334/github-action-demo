using PEXC.Case.Domain;
using PEXC.Common.Taxonomy.DataContracts;

namespace PEXC.Case.Services;

public class TaxonomyService : ITaxonomyService
{
    private readonly IReadOnlyDictionary<Guid, TermDto> _industryTags;
    private readonly IReadOnlyDictionary<Guid, TermDto> _capabilityTags;

    internal IReadOnlyDictionary<int, TermDto> Offices { get; init; }
    internal IReadOnlyDictionary<int, TermDto> Industries { get; init; }
    internal IReadOnlyDictionary<int, TermDto> Capabilities { get; init; }

    public TaxonomyService(
        IReadOnlyDictionary<int, TermDto> offices,
        IReadOnlyDictionary<int, TermDto> industries,
        IReadOnlyDictionary<int, TermDto> capabilities)
    {
        Offices = offices;
        Industries = industries;
        Capabilities = capabilities;
        _industryTags = ToTagIdDictionary(Industries.Values);
        _capabilityTags = ToTagIdDictionary(Capabilities.Values);
    }

    public TaxonomyItem MapCapabilityTagId(string? tagId)
        => MapTagIdInternal(tagId, _capabilityTags);

    public TaxonomyItem MapIndustryTagId(string? tagId)
        => MapTagIdInternal(tagId, _industryTags);

    public TaxonomyItem? MapIndustryTaxonomy(TaxonomyItem? taxonomyItem)
        => MapTaxonomy(Industries, taxonomyItem);

    public TaxonomyItem? MapIndustryTaxonomy(int? taxonomyId)
        => MapTaxonomy(Industries, taxonomyId);

    public TaxonomyItem? MapCapabilityTaxonomy(TaxonomyItem? taxonomyItem)
        => MapTaxonomy(Capabilities, taxonomyItem);

    public TaxonomyItem? MapCapabilityTaxonomy(int? taxonomyId)
        => MapTaxonomy(Capabilities, taxonomyId);

    public TaxonomyOffice? MapOfficeTaxonomy(TaxonomyOffice? taxonomyOffice)
        => MapOfficeTaxonomy(taxonomyOffice?.Code) ?? taxonomyOffice;

    public TaxonomyOffice? MapOfficeTaxonomy(int? officeCode)
        => officeCode.HasValue && Offices.TryGetValue(officeCode.Value, out var office)
            ? new TaxonomyOffice(office.OfficeCode, office.Name, office.OfficeCluster, GetRegion(office))
            : null;

    public IReadOnlyList<string?> MapIndustryTaxonomyPath(TaxonomyItem taxonomyItem)
        => taxonomyItem.Id.HasValue && Industries.TryGetValue(taxonomyItem.Id.Value, out var term)
            ? GetPath(term).Select(p => p.Name).ToList()
            : new[] { null, null, taxonomyItem.Name };

    private static TaxonomyItem? MapTaxonomy(IReadOnlyDictionary<int, TermDto> taxonomies, TaxonomyItem? taxonomyItem)
        => MapTaxonomy(taxonomies, taxonomyItem?.Id) ?? taxonomyItem;

    private static TaxonomyItem? MapTaxonomy(IReadOnlyDictionary<int, TermDto> taxonomies, int? taxonomyId)
        => taxonomyId.HasValue && taxonomies.TryGetValue(taxonomyId.Value, out var term)
            ? new TaxonomyItem(term.Id, term.Name)
            : null;

    private static IReadOnlyDictionary<Guid, TermDto> ToTagIdDictionary(IEnumerable<TermDto> taxonomy)
        => taxonomy
            .Where(p => p.TagId.HasValue)
            .ToDictionary(p => p.TagId.GetValueOrDefault());

    private static TaxonomyItem MapTagIdInternal(string? tagId, IReadOnlyDictionary<Guid, TermDto> dict)
    {
        if (!Guid.TryParse(tagId, out var id))
            return TaxonomyItem.Empty;

        return dict.TryGetValue(id, out var term)
            ? new TaxonomyItem(term.Id, term.Name)
            : TaxonomyItem.Empty;
    }

    private static IEnumerable<TermDto> GetPath(TermDto term)
    {
        var result = new List<TermDto>();

        while (term != null)
        {
            result.Add(term);
            term = term.Parent;
        }

        result.Reverse();

        return result;
    }

    private static string? GetRegion(TermDto office)
        => GetPath(office).FirstOrDefault()?.Name;
}