using PEXC.Case.DataContracts.V1;
using PEXC.Case.DataContracts.V1.CaseForms;
using PEXC.Case.Domain;
using PEXC.Case.Services.Workflow;
using PEXC.Common.BaseApi.Profile.DataContracts.V1;

namespace PEXC.Case.Tests.Common;

public static class Fake
{
    public static string Guid() => System.Guid.NewGuid().ToString();

    public static CaseEntity CaseEntity(
        string id = "",
        RelationshipType relationshipType = RelationshipType.NonRetainer,
        string? key = null) =>
        new(string.IsNullOrEmpty(id) ? Guid() : id, "CaseCode", relationshipType)
        {
            Created = DateTime.UtcNow,
            Key = key ?? "OldCaseCode",
            CreatedBy = new UserInfo(UserType.User, "username") { UserEcode = "eCode1" },
            Modified = DateTime.UtcNow,
            ModifiedBy = new UserInfo(UserType.User, "username") { UserEcode = "eCode2" },
            ItemStage = CaseState.New,
            PrimaryCapability = new TaxonomyItem(123, "Cost optimization"),
            CaseName = "CaseName",
            PrimaryIndustry = new TaxonomyItem(456, "Aviation"),
            ClientName = "ClientName",
            ManagingOffice = new TaxonomyOffice(110, "Boston", "Boston", "North America"),
            ClientHeadEcode = "eCode1",
            BillingPartnerEcode = "eCode2",
            OperatingPartnerEcodes = new List<string> { "eCode3", "eCode4" },
            AdvisorsEcodes = new List<string> { "eCode1", "eCode2" },
            ManagerEcode = "eCode4",
            StartDate = DateTime.UtcNow.AddDays(-3),
            EndDate = DateTime.UtcNow.AddDays(-1),
            TargetName = "TargetName",
            TargetDescription = "TargetDescription",
            TargetCountry = "TargetCountry",
            CorrelationId = System.Guid.NewGuid(),
            SecondaryIndustries = new List<TaxonomyItem> { new(111, "111"), new(222, "222") },
            BainExpertsEcodes = new List<string> { "eCode5", "eCode6" },
            LeadKnowledgeSpecialistEcode = "eCode6",
            AdvancedAnalyticsUsage = new List<string> { "Linkedin", "Yipit" },
            BainExpertGroupsEcosystemPartnerUsage = new List<string> { "BCN", "BBN" },
            OpsMarginImprovement = "OpsMarginImprovement",
            OpsMarginImprovementDetails = new List<string> { "G&A", "CAPEX optimization" },
            PrimaryResearch = new List<string> { "R1", "R2" },
            PrimaryResearchRecommendation1 = new ResearchRecommendation("Rec1", 1),
            PrimaryResearchRecommendation2 = new ResearchRecommendation(null, 2),
            PrimaryResearchRecommendation3 = new ResearchRecommendation("Rec3", null),
            Vdd = true,
            SurveyUsage = true,
            ClosedDeal = true,
            ExternalAdvisorUsage = true,
            A1VcpInclusion = true,
            EsgConsideration = true,
            AagGuidanceOnVendor = true,
            AdvancedAnalyticsAltDataToolOutputSatisfaction = 5,
            PrimaryResearchOutputSatisfaction = 3,
            IndustrySectorsAnalyzed = "Industries Analyzed",
            OpsDdDuration = "2-3 weeks",
            OpsDdTeam = "Ops DD Team",
            HistoricFields = new CaseHistoricFieldsEntity
            {
                OpsDdComponent = "20%"
            },
            IsInConfidentialGracePeriod = false,
            UniqueId = CaseDocumentHelper.GenerateUniqueId("CaseCode", "CaseName", relationshipType),
            Year = "2020",
            Timestamp = 15,
            ETag = "my-tag"
        };

    public static CaseEditDto CaseEditDtoFromEntity(CaseEntity caseEntity, bool? published = null) =>
        new(caseEntity.Id, caseEntity.Key, caseEntity.CaseCode, caseEntity.RelationshipType, published, caseEntity.ItemStage)
        {
            CaseDetailsSection = CaseDetailsSectionFromEntity(caseEntity),
            TargetDetailsSection = TargetDetailsSectionFromEntity(caseEntity),
        };

    private static TargetDetailsSectionDto TargetDetailsSectionFromEntity(CaseEntity caseEntity) =>
        new()
        {
            TargetDetailsAndFinalDocumentsSection = new TargetDetailsAndFinalDocumentsSectionDto
            {
                TargetName = caseEntity.TargetName,
                TargetDescription = caseEntity.TargetDescription,
                TargetCountry = caseEntity.TargetCountry,
                SurveyUsage = caseEntity.SurveyUsage,
                SecondaryIndustries = caseEntity.SecondaryIndustries?.Select(i => new TaxonomyItemDto(i.Id, i.Name)).ToList(),
                SecondaryIndustriesPaths = caseEntity.SecondaryIndustries?.Select(i => new TaxonomyPathDto(new[] { i.Name! })).ToList(),
                ClosedDeal = caseEntity.ClosedDeal,
                Vdd = caseEntity.Vdd,
                TargetPubliclyTraded = caseEntity.TargetPubliclyTraded,
                Sensitive = caseEntity.Sensitive,
            },
            UseOfExpertsSection = CreateUseOfExpertsSection(caseEntity),
            ScopeOfDiligenceSection = CreateScopeOfDiligenceSection(caseEntity),
            UseOfToolsSection = CreateUseOfToolsSection(caseEntity),
        };

    private static CaseDetailsSectionDto CaseDetailsSectionFromEntity(CaseEntity caseEntity) =>
        new()
        {
            CaseName = caseEntity.CaseName,
            ClientName = caseEntity.ClientName,
            ManagingOffice = new TaxonomyOfficeDto(
                caseEntity.ManagingOffice!.Code,
                caseEntity.ManagingOffice!.Name,
                caseEntity.ManagingOffice!.OfficeCluster,
                caseEntity.ManagingOffice!.Region),
            ManagerEcode = caseEntity.ManagerEcode,
            PrimaryIndustry = new TaxonomyItemDto(caseEntity.PrimaryIndustry!.Id, caseEntity.PrimaryIndustry.Name),
            PrimaryCapability = new TaxonomyItemDto(caseEntity.PrimaryCapability!.Id, caseEntity.PrimaryCapability.Name),
            ClientHeadEcode = caseEntity.ClientHeadEcode,
            BillingPartnerEcode = caseEntity.BillingPartnerEcode,
            OperatingPartnerEcodes = caseEntity.OperatingPartnerEcodes,
            LeadKnowledgeSpecialistEcode = caseEntity.LeadKnowledgeSpecialistEcode,
            StartDate = caseEntity.StartDate,
            EndDate = caseEntity.EndDate,
        };

    public static CaseCreateDto CaseCreateDtoFromEntity(CaseEntity caseEntity) =>
        new(caseEntity.CaseCode)
        {
            CaseDetailsSection = CaseDetailsSectionFromEntity(caseEntity),
            TargetDetailsSection = TargetDetailsSectionFromEntity(caseEntity),
        };

    public static SurveyDto SurveyDtoFromEntity(CaseEntity caseEntity) =>
        new(caseEntity.Id, caseEntity.Key, caseEntity.CaseCode, caseEntity.RelationshipType)
        {
            SurveyCaseDetailsSection = new SurveyCaseDetailsSectionDto
            {
                CaseName = caseEntity.CaseName,
                ClientName = caseEntity.ClientName,
                CaseCode = caseEntity.CaseCode,
                StartDate = caseEntity.StartDate,
                EndDate = caseEntity.EndDate,
            },
            SurveyTargetDetailsSection = new SurveyTargetDetailsSectionDto
            {
                TargetDetailsAndFinalDocumentsSection = new SurveyTargetDetailsAndFinalDocumentsSectionDto
                {
                    TargetName = caseEntity.TargetName,
                    TargetDescription = caseEntity.TargetDescription,
                    TargetCountry = caseEntity.TargetCountry,
                    SurveyUsage = caseEntity.SurveyUsage,
                    TargetPubliclyTraded = caseEntity.TargetPubliclyTraded,
                },
                UseOfExpertsSection = new UseOfExpertsSectionDto
                {

                    BainExpertsEcodes = caseEntity.BainExpertsEcodes,
                    ExternalAdvisorUsage = caseEntity.ExternalAdvisorUsage,
                    ExternalAdvisorUsageDetails = caseEntity.ExternalAdvisorUsageDetails,
                },
                ScopeOfDiligenceSection = new ScopeOfDiligenceSectionDto
                {

                    A1VcpInclusion = caseEntity.A1VcpInclusion,
                    OpsMarginImprovement = caseEntity.OpsMarginImprovement,
                    OpsMarginImprovementDetails = caseEntity.OpsMarginImprovementDetails,
                    EsgConsideration = caseEntity.EsgConsideration,
                },
                UseOfToolsSection = new UseOfToolsSectionDto
                {
                    BainExpertGroupsEcosystemPartnerUsage = caseEntity.BainExpertGroupsEcosystemPartnerUsage,
                    AdvancedAnalyticsUsage = caseEntity.AdvancedAnalyticsUsage,
                    PrimaryResearch = caseEntity.PrimaryResearch,

                    AdvancedAnalyticsAltDataToolOutputSatisfaction = caseEntity.AdvancedAnalyticsAltDataToolOutputSatisfaction,
                    PrimaryResearchOutputSatisfaction = caseEntity.PrimaryResearchOutputSatisfaction,
                    PrimaryResearchRecommendation1 = new ResearchRecommendationDto(caseEntity.PrimaryResearchRecommendation1?.Name, caseEntity.PrimaryResearchRecommendation1?.Rating),
                    PrimaryResearchRecommendation2 = new ResearchRecommendationDto(caseEntity.PrimaryResearchRecommendation2?.Name, caseEntity.PrimaryResearchRecommendation2?.Rating),
                    PrimaryResearchRecommendation3 = new ResearchRecommendationDto(caseEntity.PrimaryResearchRecommendation3?.Name, caseEntity.PrimaryResearchRecommendation3?.Rating),
                    AagGuidanceOnVendor = caseEntity.AagGuidanceOnVendor,
                }
            }
        };

    private static UseOfExpertsSectionDto CreateUseOfExpertsSection(CaseEntity caseEntity) =>
        new()
        {
            BainExpertsEcodes = caseEntity.BainExpertsEcodes,
            ExternalAdvisorUsage = caseEntity.ExternalAdvisorUsage,
            ExternalAdvisorUsageDetails = caseEntity.ExternalAdvisorUsageDetails,
        };

    private static UseOfToolsSectionDto CreateUseOfToolsSection(CaseEntity caseEntity) =>
        new()
        {
            BainExpertGroupsEcosystemPartnerUsage = caseEntity.BainExpertGroupsEcosystemPartnerUsage,
            AdvancedAnalyticsUsage = caseEntity.AdvancedAnalyticsUsage,
            PrimaryResearch = caseEntity.PrimaryResearch,

            AdvancedAnalyticsAltDataToolOutputSatisfaction = caseEntity.AdvancedAnalyticsAltDataToolOutputSatisfaction,
            PrimaryResearchOutputSatisfaction = caseEntity.PrimaryResearchOutputSatisfaction,
            PrimaryResearchRecommendation1 = new ResearchRecommendationDto(caseEntity.PrimaryResearchRecommendation1?.Name, caseEntity.PrimaryResearchRecommendation1?.Rating),
            PrimaryResearchRecommendation2 = new ResearchRecommendationDto(caseEntity.PrimaryResearchRecommendation2?.Name, caseEntity.PrimaryResearchRecommendation2?.Rating),
            PrimaryResearchRecommendation3 = new ResearchRecommendationDto(caseEntity.PrimaryResearchRecommendation3?.Name, caseEntity.PrimaryResearchRecommendation3?.Rating),
            AagGuidanceOnVendor = caseEntity.AagGuidanceOnVendor
        };

    private static ScopeOfDiligenceSectionDto CreateScopeOfDiligenceSection(CaseEntity caseEntity) =>
        new()
        {
            A1VcpInclusion = caseEntity.A1VcpInclusion,
            OpsMarginImprovement = caseEntity.OpsMarginImprovement,
            OpsMarginImprovementDetails = caseEntity.OpsMarginImprovementDetails,
            EsgConsideration = caseEntity.EsgConsideration
        };

    public static SurveyDto SurveyDto(string id = "") =>
        SurveyDtoFromEntity(CaseEntity(id));

    public static CaseSearchItemDto CaseSearchItemDto(string id) => new(id, "Key", "CaseCode");

    public static CaseManagementItemDto CaseManagementItemDto(string id) => new(id, "Key", "CaseCode");

    public static EmployeeDetailsDto EmployeeDetails(string eCode)
        => new(eCode, "John", "Smith", "John " + eCode, "a@bain.com", "C4", "Boston", false, "BostonOfficeName");
}