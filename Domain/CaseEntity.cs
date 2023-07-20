using Newtonsoft.Json;

namespace PEXC.Case.Domain;

public class CaseEntity : IEntity
{
    public CaseEntity(
        string id,
        string caseCode,
        RelationshipType relationshipType)
    {
        Id = id;
        CaseCode = caseCode.Trim();
        Key = caseCode.Trim();
        RelationshipType = relationshipType;
        HistoricFields = new CaseHistoricFieldsEntity();
        Permissions = new List<Permission>();
    }

    public string Key { get; set; }
    public string Type => nameof(CaseEntity);

    public Guid CorrelationId { get; set; }

    public string Id { get; set; }
    public string UniqueId { get; set; } = null!;
    public CaseState ItemStage { get; set; }
    public DateTime? Created { get; set; }
    public UserInfo? CreatedBy { get; set; }
    public DateTime? Modified { get; set; }
    public UserInfo? ModifiedBy { get; set; }
    public RelationshipType RelationshipType { get; set; }
    public string CaseCode { get; set; }
    public TaxonomyItem? PrimaryCapability { get; set; }
    public string? CaseName { get; set; }
    public TaxonomyItem? PrimaryIndustry { get; set; }
    public string? ClientName { get; set; }
    public string? ClientId { get; set; }
    public TaxonomyOffice? ManagingOffice { get; set; }
    public string? ClientHeadEcode { get; set; }
    public string? BillingPartnerEcode { get; set; }
    public List<string>? OperatingPartnerEcodes { get; set; }
    public List<string>? AdvisorsEcodes { get; set; }
    public string? ManagerEcode { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Year { get; set; }
    public string? TargetName { get; set; }
    public string? TargetDescription { get; set; }
    public string? TargetCountry { get; set; }
    public string? KmContactName { get; set; }
    public string? LeadKnowledgeSpecialistEcode { get; set; }
    public string? OneNotifications { get; set; }
    public List<TaxonomyItem>? SecondaryIndustries { get; set; }
    public List<TaxonomyItem>? SecondaryCapabilities { get; set; }
    public string? Keyword { get; set; }
    public FinalMaterialAvailable FinalMaterialAvailable { get; set; }
    public bool? ClosedDeal { get; set; }
    public string? DealStatus { get; set; }
    public string? OpsCommentReviewed { get; set; }
    public string? OpsDdComments { get; set; }
    public string? AdditionalComments { get; set; }
    public bool? Vdd { get; set; }
    public string? MainCompetitorsAnalyzed { get; set; }
    public string? GeographicRegion { get; set; }
    public bool? A1VcpInclusion { get; set; }
    public bool? AagGuidanceOnVendor { get; set; }
    public string? AdditionalScopeComments { get; set; }
    public List<string>? AdvancedAnalyticsUsage { get; set; }
    public short? AdvancedAnalyticsAltDataToolOutputSatisfaction { get; set; }
    public List<string>? BainExpertGroupsEcosystemPartnerUsage { get; set; }
    public List<string>? BainExpertsEcodes { get; set; }
    public string? CommercialExcellenceAssessment { get; set; }
    public string? DisruptionAssessment { get; set; }
    public bool? EsgConsideration { get; set; }
    public bool? ExternalAdvisorUsage { get; set; }
    public string? ExternalAdvisorUsageDetails { get; set; }
    public string? HighlightedCaseAspects { get; set; }
    public string? ItDigitalAssessment { get; set; }
    public string? MarketingECommerce { get; set; }
    public string? OpsMarginImprovement { get; set; }
    public List<string>? OpsMarginImprovementDetails { get; set; }
    public ResearchRecommendation? PrimaryResearchRecommendation1 { get; set; }
    public ResearchRecommendation? PrimaryResearchRecommendation2 { get; set; }
    public ResearchRecommendation? PrimaryResearchRecommendation3 { get; set; }
    public List<string>? PrimaryResearch { get; set; }
    public short? PrimaryResearchOutputSatisfaction { get; set; }
    public bool? SurveyUsage { get; set; }
    [JsonIgnore]
    public bool IsDeleted => ItemStage == CaseState.Deleted;
    public bool? IsSearchable { get; set; }
    public bool? IsInConfidentialGracePeriod { get; set; }
    public bool DataConfirmation { get; set; }
    public bool? TargetPubliclyTraded { get; set; }
    public bool? Sensitive { get; set; }
    public string? IndustrySectorsAnalyzed { get; set; }
    public string? OpsDdDuration { get; set; }
    public string? OpsDdTeam { get; set; }

    public CaseHistoricFieldsEntity? HistoricFields { get; set; }
    public SharePointDirectoryEntity? SharePointDirectory { get; set; }

    public List<Permission>? Permissions { get; set; }

    [JsonProperty(PropertyName = "_ts")]
    public long Timestamp { get; set; }

    [JsonProperty(PropertyName = "_etag")]
    public string ETag { get; set; } = null!;
}