namespace PEXC.Case.Services.IRIS.Contracts;

public record IrisCaseDto(string CaseCode)
{
   public string? CaseOffice { get; init; }
   public string? BillingOffice{ get; init; }
   public DateTime? ModifiedDate{ get; init; }
   public string? ModifiedBy{ get; init; }
   public int? PrimaryIndustry{ get; init; }
   public int? PrimaryCapability{ get; init; }
   public IReadOnlyList<int>? SecondaryIndustries{ get; init; }
   public IReadOnlyList<int>? SecondaryCapabilities{ get; init; }
   public string? LeadKnowledgeSpecialist{ get; init; }
   public string? CapabilityKnowledgeSpecialist{ get; init; }
   public string? IndustryKnowledgeSpecialist{ get; init; }
   public bool IsPublished{ get; init; }
   public bool IsDeleted { get; init; }
}