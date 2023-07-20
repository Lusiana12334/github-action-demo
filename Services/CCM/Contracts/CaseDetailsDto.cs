namespace PEXC.Case.Services.CCM.Contracts;

public record CaseDetailsDto(string CaseCode)
{
    public string? CaseName { get; init; }
    public int ClientId { get; init; }
    public string? ClientName { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public DateTime? LastUpdated { get; init; }
    public int CaseOffice { get; init; }
    public string? GlobalCoordinatingPartner { get; init; }
    public string? BillingPartner { get; init; }
    public string? CaseManager { get; init; }
    public string? PrimaryIndustryTagId { get; init; }
    public string? PrimaryCapabilityTagId { get; init; }
    public string? LeadKnowledgeSpecialistEcode { get; set; }
    public IEnumerable<TaxonomyTerm> SecondaryCapability { get; init; } = Enumerable.Empty<TaxonomyTerm>();
    public IEnumerable<TaxonomyTerm> SecondaryIndustry { get; init; } = Enumerable.Empty<TaxonomyTerm>();

    public record TaxonomyTerm(string TagId);
}