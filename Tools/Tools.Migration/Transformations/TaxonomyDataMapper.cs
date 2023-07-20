using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PEXC.Case.Domain;
using PEXC.Case.Services.CCM.Contracts;
using PEXC.Common.Taxonomy;
using PEXC.Common.Taxonomy.DataContracts;

namespace PEXC.Case.Tools.Migration.Transformations;

public class TaxonomyDataMapper
{
    private static readonly Dictionary<string, int> MissingTaxonomyMapping =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "Internet Media", 857 },
            { "Cards & Payments", 623 },
            { "Apparel & Footwear Retailers", 790 },
            { "Utilities & Alternative Energy", 583 },
            { "Computers & Peripherals", 846 },
            { "Consumer Software", 874 },
            { "Food, Drug & Convenience Stores", 797 },
            { "Consumer Electronics", 3755 },
            { "Data Centers & Web Hosting", 860 },
            { "Engineering, Procurement and Construction", 371 },
            { "Food Stores", 800 },
            { "Payment Processing and Merchant Acquiring", 3809 },
            { "Credit Cards", 3810 },
            { "Third Party Logistics", 349 },
            { "Railway", 342 },
            { "Logistics & Transport", 335 },
            { "Freight Forwarding Services", 338 },
            { "Advanced Manufacturing & Services", 311 },
            { "Other Advanced Manufacturing & Services", 311 },
            { "Logistics Transportation Infrastructure", 341 },
            { "Payments Infrastructure", 632 },
            { "Airline Ground Services", 332 },
            { "Air Cargo & Air Freight", 329 },
            { "Wholesale Banking", 607 },
            { "Primary & Secondary Schools", 835 },
            { "Power & Gas Utilities", 584 },
            { "Handsets", 852 },
            { "Wind", 3651 },
            { "Airline Technology Services", 333 },
            { "Lubricants", 579 },
            { "Retail", 789 },
            { "Internet Retailers", 789 },
            { "Non-Store Retailers", 789 },
            { "Other Social Impact", 839 },
            { "Express, Parcel & Postal Service", 338 },
            { "Power Generation", 591 },
            { "Commercial/Corporate Banking", 608 },
            { "Healthcare", 678},
            { "Enterprise Software", 873},
            { "IT Services", 858},
            { "Venture Capital Fund", 788},
            { "Development Tools", 873},
            { "Digital Gaming", 433},
            { "Internet & Broadband Access", 421},
            { "Commercial Aviation Parts & Systems Suppliers", 317},
            { "IT Consulting & Systems Integration", 4910},
            { "IT Maintenance & Support", 4910},
            { "Wired Network Operations", 421},
            { "Crop Production", 490},
            { "Other Automotive and Mobility", 359},
            { "Commercial Air Transport", 315},
            { "Poultry Processing", 515},
            { "Business & General Aviation", 314},
            { "IT Outsourcing", 4910},
            { "Gaming", 433},
            { "Bus, Taxi & Other Transport Services", 336},
            { "Meat, Poultry & Fish Processing", 511},
            { "Space Satellites", 326},
            { "Independent Financial Advisory (IFAs) and other Wealth Advisory", 649},
            { "Defense Systems Integration & Services", 322},

        };

    private readonly ITaxonomyRepository _taxonomyRepository;

    private readonly MigrationContext _context;

    private readonly ILogger<TaxonomyDataMapper> _logger;

    private Dictionary<string, TermDto> _offices = null!;

    private Dictionary<string, TermDto> _industry = null!;

    private Dictionary<string, TermDto> _capability = null!;

    private Dictionary<string, TermDto> _missingTaxonomyTerms = null!;

    private Dictionary<Guid, TermDto> _industryTagMapping = null!;

    private Dictionary<Guid, TermDto> _capabilityTagMapping = null!;

    private Dictionary<int, TermDto> _officesByCode = null!;

    private TermDto? _washingtonOffice;

    private bool _loaded;

    public TaxonomyDataMapper(
        ITaxonomyRepository taxonomyRepository,
        MigrationContext context,
        ILogger<TaxonomyDataMapper> logger)
    {
        _taxonomyRepository = taxonomyRepository;
        _context = context;
        _logger = logger;
    }

    public async Task Init()
    {
        _logger.LogInformation("Loading taxonomy from Taxonomy Service");
        var sw = Stopwatch.StartNew();
        await InitInternal();
        sw.Stop();
        _logger.LogInformation("Loaded taxonomy in {time}", sw.Elapsed);

        _loaded = true;
    }

    private async Task InitInternal()
    {
        var offices = await _taxonomyRepository.GetFlatTaxonomy(TaxonomyType.Office);
        var stringComparer = StringComparer.OrdinalIgnoreCase;

        _industry = (await _taxonomyRepository.GetFlatTaxonomy(TaxonomyType.Industry))
            .ToDictionary(p => p.Value.Name, p => p.Value, stringComparer);
        _industryTagMapping =
            _industry.Where(p => p.Value.TagId.HasValue).ToDictionary(p => p.Value.TagId.GetValueOrDefault(), p => p.Value);
        _capability = (await _taxonomyRepository.GetFlatTaxonomy(TaxonomyType.Capability))
            .ToDictionary(p => p.Value.Name, p => p.Value, stringComparer);
        _capabilityTagMapping =
            _capability.Where(p => p.Value.TagId.HasValue)
                .ToDictionary(p => p.Value.TagId.GetValueOrDefault(), p => p.Value);
        _offices = offices.DistinctBy(p => p.Value.Name).ToDictionary(p => p.Value.Name, p => p.Value, stringComparer);
        _officesByCode = offices.Where(o => o.Value.OfficeCode != 0).ToDictionary(p => p.Value.OfficeCode, p => p.Value);
        _washingtonOffice = _officesByCode.GetValueOrDefault(116);


        var mappingTerms = MissingTaxonomyMapping.ToDictionary(p => p.Key, FindIndustryById, stringComparer);
        var idsWithoutTerms = mappingTerms.Where(p => p.Value == null).ToList();

        if (idsWithoutTerms.Count > 0)
        {
            var itemList = string.Join(";", idsWithoutTerms.Select(p => $"'{p.Key}'"));
            throw new InvalidOperationException($"Missing terms: [{itemList}]");
        }

        _missingTaxonomyTerms = mappingTerms.ToDictionary(p => p.Key, p => p.Value!);
    }

    private TermDto? FindIndustryById(KeyValuePair<string, int> p)
    {
        return _industry.Values.FirstOrDefault(t => t.Id == p.Value)
               ?? _capability.Values.FirstOrDefault(t => t.Id == p.Value)!;
    }

    public void ChangeTaxonomy(CaseEntity caseEntity, LeapMasterRecord leapMaster, CaseDetailsDto? ccmData)
    {
        if (!_loaded) throw new InvalidOperationException("Please call Init() method first");

        if (caseEntity.RelationshipType == RelationshipType.NonRetainer)
            MapNonRetainer(caseEntity, leapMaster, ccmData);
        else
            MapRetainer(caseEntity, leapMaster);
    }

    private void MapRetainer(CaseEntity caseEntity, LeapMasterRecord leapMaster)
    {
        caseEntity.PrimaryIndustry = GetTaxonomyItem(leapMaster, nameof(LeapMasterRecord.ClientType), _industry);
        caseEntity.PrimaryCapability = GetTaxonomyItem(leapMaster, nameof(LeapMasterRecord.CaseType), _capability);
        caseEntity.ManagingOffice = GetOffice(leapMaster, _offices, leapMaster.ManagingOffice!);
        caseEntity.SecondaryIndustries = MapSecondaryIndustries(leapMaster);
    }

    private void MapNonRetainer(CaseEntity caseEntity, LeapMasterRecord leapMaster, CaseDetailsDto? ccmCaseData)
    {
        if (ccmCaseData == null)
        {
            _context.AddMissingCcmRecord(leapMaster);
        }

        caseEntity.PrimaryIndustry = GetTaxonomyItem(leapMaster, nameof(LeapMasterRecord.ClientType),
            ccmCaseData?.PrimaryIndustryTagId, _industryTagMapping, _industry);
        caseEntity.PrimaryCapability = GetTaxonomyItem(leapMaster, nameof(LeapMasterRecord.CaseType),
            ccmCaseData?.PrimaryCapabilityTagId, _capabilityTagMapping, _capability);
        int? caseOffice = ccmCaseData?.CaseOffice;

        caseEntity.ManagingOffice = caseOffice.HasValue
            ? GetOffice(leapMaster, _officesByCode, caseOffice.GetValueOrDefault())
            : GetOffice(leapMaster, _offices, leapMaster.ManagingOffice);

        // Secondary industries *always* come from Migration data, not from CCM
        caseEntity.SecondaryIndustries = MapSecondaryIndustries(leapMaster);
    }

    private TaxonomyOffice GetOffice<T>(LeapMasterRecord leapMaster, Dictionary<T, TermDto> offices, T? office) where T : notnull
    {
        if (office is not null && offices.TryGetValue(office, out var off))
            return new TaxonomyOffice(off.OfficeCode, off.Name, off.OfficeCluster, GetRegion(off));

        if (office?.ToString() == "Washington, D.C."
            && _washingtonOffice != null)
        {
            return new TaxonomyOffice(_washingtonOffice.OfficeCode, _washingtonOffice.Name,
                _washingtonOffice.OfficeCluster, GetRegion(_washingtonOffice));
        }

        // For non-retainer it means that we were not able to call CCM, so this error has been already reported.
        if (leapMaster.RelationshipType == RelationshipType.Retainer)
            _context.AddUnmapped(leapMaster, nameof(LeapMasterRecord.ManagingOffice), office?.ToString() ?? "Unknown office");

        return new TaxonomyOffice(null, leapMaster.ManagingOffice!, leapMaster.CombinedOffices, leapMaster.Region);
    }

    private TaxonomyItem GetTaxonomyItem(
        LeapMasterRecord record,
        string propertyName,
        string? strTagId,
        Dictionary<Guid, TermDto> taxonomy,
        Dictionary<string, TermDto> defaultTaxonomy
        )
    {
        if (strTagId != null)
        {
            if (!Guid.TryParse(strTagId, out var tagId))
                throw new InvalidOperationException("This should never happen");

            if (taxonomy.TryGetValue(tagId, out var termDto))
            {
                var leapTaxonomyName = record.GetProperty(propertyName);
                if (_missingTaxonomyTerms.TryGetValue(leapTaxonomyName, out var term))
                    leapTaxonomyName = term.Name;

                if (!StringComparer.OrdinalIgnoreCase.Equals(leapTaxonomyName, termDto.Name))
                    _context.AddDifferentTaxonomy(record, propertyName, leapTaxonomyName, termDto.Name);
                return new TaxonomyItem(termDto.Id, termDto.Name);
            }

            if (StringComparer.OrdinalIgnoreCase.Equals(record.CaseCode, "0KKH"))
                return new TaxonomyItem(678, "Healthcare & Life Sciences");

            _context.MissingTaxonomyTagId(record, propertyName, tagId);
        }

        // For Non-Retainer if ccm data is missing we just map using Poolparty and text (confirmed by MV)
        return GetTaxonomyItem(record, propertyName, defaultTaxonomy);
    }

    private List<TaxonomyItem> MapSecondaryIndustries(LeapMasterRecord leapMaster)
    {
        var industry = GetOptionalTaxonomyItem(leapMaster, nameof(LeapMasterRecord.PrimaryIndustry), _industry);
        var slIndustry = GetOptionalTaxonomyItem(leapMaster, nameof(LeapMasterRecord.SecondLevelIndustry), _industry);
        var topLevelIndustry = GetOptionalTaxonomyItem(leapMaster, nameof(LeapMasterRecord.TopLevelIndustry), _industry);

        var result = new List<TaxonomyItem>();

        if (industry != null)
            result.Add(industry);

        if (slIndustry != null && !IsAncestor(slIndustry.Id, industry)
            && !result.Exists(ti => string.Equals(ti.Name, slIndustry.Name, StringComparison.OrdinalIgnoreCase)))
            result.Add(slIndustry);

        if (topLevelIndustry != null
            && !IsAncestor(topLevelIndustry.Id, industry)
            && !IsAncestor(topLevelIndustry.Id, slIndustry)
            && !result.Exists(ti => string.Equals(ti.Name, topLevelIndustry.Name, StringComparison.OrdinalIgnoreCase)))
            result.Add(topLevelIndustry);

        return result;
    }

    private TaxonomyItem? GetOptionalTaxonomyItem(
        LeapMasterRecord record,
        string propertyName,
        Dictionary<string, TermDto> taxonomy)
    {
        var textTerm = record.GetProperty(propertyName);

        return string.IsNullOrEmpty(textTerm) ? null : GetTaxonomyItem(record, propertyName, taxonomy);
    }

    private TaxonomyItem GetTaxonomyItem(
        LeapMasterRecord record,
        string propertyName,
        Dictionary<string, TermDto> taxonomy)
    {
        var textTerm = record.GetProperty(propertyName).Trim();

        if (taxonomy.TryGetValue(textTerm, out var ind)
            || _missingTaxonomyTerms.TryGetValue(textTerm, out ind))
            return new TaxonomyItem(ind!.Id, ind!.Name);

        if (string.Equals(textTerm, "Commercial Due Diligence - Financial Investors"))
            return new TaxonomyItem(306, "Due Diligence - Financial Investors");
        
        // For non-retainer it means that we was not able to call CCM, so this error is already reported.
        if (record.RelationshipType == RelationshipType.Retainer)
            _context.AddUnmapped(record, propertyName, textTerm);

        return new TaxonomyItem(null, textTerm);
    }

    private bool IsAncestor(int? parentId, TaxonomyItem? child)
    {
        if (child?.Id == null || parentId == null)
            return false;

        _industry.TryGetValue(child.Name!, out var term);

        while (term != null)
        {
            if (term.Id == parentId)
                return true;

            term = term.Parent;
        }

        return false;
    }

    private static string GetRegion(TermDto term)
    {
        while (term.Parent != null)
            term = term.Parent;

        return term.Name;
    }
}