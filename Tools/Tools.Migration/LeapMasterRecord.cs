using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using PEXC.Case.Domain;
using PEXC.Case.Tools.Migration.Csv;

namespace PEXC.Case.Tools.Migration;
#pragma warning disable CS8618

public class LeapMasterRecord
{
    [Name("Final Material Available")]
    [TypeConverter(typeof(StringEnumConverter<FinalMaterialAvailable>))]
    public FinalMaterialAvailable FinalMaterialAvailable { get; set; }

    [Name("Created")]
    public DateTime? Created { get; set; }

    [Name("Created By")]
    public string? CreatedBy { get; set; }

    [Name("DuplicateRecordForMigration")]
    public string? DuplicateRecordForMigration { get; set; }

    [Name("Modified")]
    public DateTime? Modified { get; set; }

    [Name("Modified By")]
    public string? ModifiedBy { get; set; }

    [Name("UniqueID")]
    public string? UniqueID { get; set; }

    [Name("Case Code")]
    public string CaseCode { get; set; }

    [Name("Case Name")]
    public string? CaseName { get; set; }

    [Name("Client Name")]
    public string? ClientName { get; set; }

    [Name("Region")]
    public string? Region { get; set; }

    [Name("Managing Office")]
    public string? ManagingOffice { get; set; }

    [Name("Start Date")]
    public DateTime? StartDate { get; set; }

    [Name("End Date")]
    public DateTime? EndDate { get; set; }

    [Name("Target Name")]
    public string? TargetName { get; set; }

    [Name("Target Description")]
    public string? TargetDescription { get; set; }

    [Name("Case Type")]
    public string? CaseType { get; set; }

    [Name("Client Type")]
    public string? ClientType { get; set; }

    [Name("Manager Name")]
    public string? ManagerName { get; set; }

    [Name("Client Head Name")]
    public string? ClientHeadName { get; set; }

    [Name("Billing Partner Name")]
    public string? BillingPartnerName { get; set; }

    [Name("Operating Partner Name")]
    public string? OperatingPartnerName { get; set; }

    [Name("KM Contact Name")]
    public string? KMContactName { get; set; }

    [Name("Additional Comments")]
    public string? AdditionalComments { get; set; }

    [Name("Target Country")]
    public string? TargetCountry { get; set; }

    [Name("Top Level Industry")]
    public string? TopLevelIndustry { get; set; }

    [Name("Second Level Industry")]
    public string? SecondLevelIndustry { get; set; }

    [Name("Primary Industry")]
    public string? PrimaryIndustry { get; set; }

    [Name("Year")]
    public string? Year { get; set; }

    [Name("Keyword")]
    public string? Keyword { get; set; }

    [Name("Main competitors analyzed as part of DD")]
    public string? MainCompetitorsAnalyzedAsPartOfDd { get; set; }

    [Name("Any Other Analysis")]
    public string? AnyOtherAnalysis { get; set; }

    [Name("Closed Deal")]
    public bool? ClosedDeal { get; set; }

    [Name("Combined Offices")]
    public string? CombinedOffices { get; set; }

    [Name("Digital Component?")]
    public string? DigitalComponent { get; set; }

    [Name("Digital Focus Comments")]
    public string? DigitalFocusComments { get; set; }

    [Name("Geographic region as part of DD")]
    public string? GeographicRegionAsPartOfDd { get; set; }

    [Name("Industry sectors analyzed as part of DD")]
    public string? IndustrySectorsAnalyzedAsPartOfDd { get; set; }

    [Name("Operational Component")]
    public string? OperationalComponent { get; set; }

    [Name("Ops DD Comments")]
    public string? OpsDdComments { get; set; }

    [Name("OpsCommentReviewed")]
    public bool? OpsCommentReviewed { get; set; }

    [Name("Relationship Type")]
    [TypeConverter(typeof(StringEnumConverter<RelationshipType>))]
    public RelationshipType RelationshipType { get; set; }

    [Name("SearchReady")]
    public bool? SearchReady { get; set; }

    [Name("Cost: IT optimization")]
    public bool? CostItOptimization { get; set; }

    [Name("Cost: capability sourcing")]
    public bool? CostCapabilitySourcing { get; set; }

    [Name("Cost: G&A")]
    public bool? CostGandA { get; set; }

    [Name("Cost: manufacturing")]
    public bool? CostManufacturing { get; set; }

    [Name("Cost: procurement")]
    public bool? CostProcurement { get; set; }

    [Name("Cost: product complexity")]
    public bool? CostProductComplexity { get; set; }

    [Name("Cost: supply chain/distribution")]
    public bool? CostSupplyChainDistribution { get; set; }

    [Name("Costs: R&D")]
    public bool? CostRnD { get; set; }

    [Name("Costs: Service optimization")]
    public bool? CostServiceOptimization { get; set; }

    [Name("Cash: CAPEX optimization")]
    public bool? CashCapexOptimization { get; set; }

    [Name("Cash: NWC optimization")]
    public bool? CashNwcOptimization { get; set; }

    [Name("Existing business growth: pricing")]
    public bool? ExistingBusinessGrowthPricing { get; set; }

    [Name("Existing business growth: sales-force")]
    public bool? ExistingBusinessGrowthSalesForce { get; set; }

    [Name("1-Notifications")]
    public string? Notifications { get; set; }

    [Name("AAG guidance on vendor or survey?")]
    public bool? AAGGuidanceOnVendorOrSurvey { get; set; }

    [Name("Additional scope comments")]
    public string? AdditionalScopeComments { get; set; }

    [Name("Advanced Analytics usage")]
    [TypeConverter(typeof(SpListConverter))]
    public IReadOnlyList<string>? AdvancedAnalyticsUsage { get; set; }

    [Name("Advanced Analytics usage details")]
    [TypeConverter(typeof(SpListConverter))]
    public IReadOnlyList<string>? AdvancedAnalyticsUsageDetails { get; set; }

    [Name("Advanced Analytics/ Alt data tool output satisfaction")]
    public short? AdvancedAnalyticsAltDataToolOutputSatisfaction { get; set; }

    [Name("APAC Did the Deal Proceed with client?")]
    public string? ApacDidTheDealProceedWithClient { get; set; }

    [Name("APAC Did the Deal Proceed with others?")]
    public string? ApacDidTheDealProceedWithOthers { get; set; }

    [Name("APAC Include in PE-DD count?")]
    public string? ApacIncludeInPEDdCount { get; set; }

    [Name("APAC Include in total case count (excluding CDD)?")]
    public string? ApacIncludeInTotalCaseCountExcludingCdd { get; set; }

    [Name("APAC Include in total case count (including CDD)?")]
    public string? ApacIncludeInTotalCaseCountIncludingCdd { get; set; }

    [Name("Bain Experts")]
    [TypeConverter(typeof(ExpertsConverter))]
    public IReadOnlyList<string> BainExpertsEcodes { get; set; }

    [Name("BillingPartnerEcode")]
    [TypeConverter(typeof(TrimConverter))]
    public string? BillingPartnerEcode { get; set; }
    
    [Name("Business definition")]
    public bool? BusinessDefinition { get; set; }

    [Name("Case Cracking Activities")]
    public string? CaseCrackingActivities { get; set; }

    [Name("Case end template (leveraging DD scale)")]
    public string? CaseEndTemplateLeveragingDdScale { get; set; }

    [Name("Case examples")]
    public string? CaseExamples { get; set; }

    [Name("CHAID")]
    public bool? CHAID { get; set; }

    [Name("ClientHeadEcode")]
    [TypeConverter(typeof(TrimConverter))]
    public string? ClientHeadEcode { get; set; }

    [Name("Commercial Excellence Assessment?")]
    public string? CommercialExcellenceAssessment { get; set; }

    [Name("Company operations review")]
    public bool? CompanyOperationsReview { get; set; }

    [Name("Competitor analysis")]
    public bool? CompetitorAnalysis { get; set; }

    [Name("Competitor review")]
    public bool? CompetitorReview { get; set; }

    [Name("Consortium")]
    public string? Consortium { get; set; }

    [Name("Consortium Details")]
    public string? ConsortiumDetails { get; set; }

    [Name("Cost: sustained cost transformation")]
    public bool? CostSustainedCostTransformation { get; set; }

    [Name("Customer analysis")]
    public bool? CustomerAnalysis { get; set; }

    [Name("Customer survey")]
    public bool? CustomerSurvey { get; set; }

    [Name("Data Confirmation")]
    public bool? DataConfirmation { get; set; }

    [Name("Deal status")]
    public string? DealStatus { get; set; }

    [Name("Derived demand")]
    public bool? DerivedDemand { get; set; }

    [Name("Disruption Assessment?")]
    public string? DisruptionAssessment { get; set; }

    [Name("Duration of Ops DD")]
    public string? DurationOfOpsDd { get; set; }

    [Name("EMEA Background")]
    public string? EmeaBackground { get; set; }

    [Name("EMEA Call Leader")]
    public string? EmeaCallLeader { get; set; }

    [Name("EMEA Date Of Call")]
    public DateTime? EmeaDateOfCall { get; set; }

    [Name("EMEA Expert Network Details")]
    public string? EmeaExpertNetworkDetails { get; set; }

    [Name("EMEA Expert Network Feedback")]
    public string? EmeaExpertNetworkFeedback { get; set; }

    [Name("EMEA Expert Network NPS")]
    public string? EmeaExpertNetworkNps { get; set; }

    [Name("EMEA Expert Network used?")]
    public string? EmeaExpertNetworkUsed { get; set; }

    [Name("EMEA External Tool Provider & Scope")]
    public string? EmeaExternalToolProviderandScope { get; set; }

    [Name("EMEA external tools utilised?")]
    public string? EmeaExternalToolsUtilised { get; set; }

    [Name("EMEA Internal Experts")]
    public string? EmeaInternalExperts { get; set; }

    [Name("EMEA Key DD Questions")]
    public string? EmeaKeyDdQuestions { get; set; }

    [Name("EMEA Key Sources")]
    public string? EmeaKeySources { get; set; }

    [Name("EMEA Selling Partners")]
    public string? EmeaSellingPartners { get; set; }

    [Name("EMEA Serving Partners")]
    public string? EmeaServingPartners { get; set; }

    [Name("EMEA Survey Provider Details")]
    public string? EmeaSurveyProviderDetails { get; set; }

    [Name("EMEA Survey Provider Feedback")]
    public string? EmeaSurveyProviderFeedback { get; set; }

    [Name("EMEA Survey Provider NPS")]
    public string? EmeaSurveyProviderNps { get; set; }

    [Name("EMEA Survey Provider Used?")]
    public string? EmeaSurveyProviderUsed { get; set; }

    [Name("ESG Consideration?")]
    public bool? ESGConsideration { get; set; }

    [Name("Existing business growth: channel management")]
    public bool? ExistingBusinessGrowthChannelManagement { get; set; }

    [Name("Existing business growth: customer")]
    public bool? ExistingBusinessGrowthCustomer { get; set; }

    [Name("Existing business growth: marketing")]
    public bool? ExistingBusinessGrowthMarketing { get; set; }

    [Name("Existing business growth: sustainable & profitable growth")]
    public bool? ExistingBusinessGrowthSustainable { get; set; }

    [Name("External Advisor Usage")]
    public bool? ExternalAdvisorUsage { get; set; }

    [Name("External Advisor Usage Details")]
    public string? ExternalAdvisorUsageDetails { get; set; }

    [Name("Industry review")]
    public bool? IndustryReview { get; set; }

    [Name("Industry trends and issues")]
    public string? IndustryTrendsAndIssues { get; set; }

    [Name("IsKMTeam")]
    public string? IsKMTeam { get; set; }

    [Name("IT / Digital Assessment?")]
    public string? ITDigitalAssessment { get; set; }

    [Name("Key Sector Codified")]
    public string? KeySectorCodified { get; set; }

    [Name("KMContactEcode")]
    [TypeConverter(typeof(TrimConverter))]
    public string? KMContactEcode { get; set; }

    [Name("Legal scrubbed version?")]
    public string? LegalScrubbedVersion { get; set; }

    [Name("ManagerEcode")]
    [TypeConverter(typeof(TrimConverter))]
    public string? ManagerEcode { get; set; }

    [Name("Marketing / E-Commerce?")]
    public string? MarketingECommerce { get; set; }

    [Name("Matching Bain Deal Database Name 1")]
    public string? MatchingBainDealDatabaseName1 { get; set; }

    [Name("Matching Bain Deal Database Name 2")]
    public string? MatchingBainDealDatabaseName2 { get; set; }

    [Name("Mgmt plan review")]
    public bool? MgmtPlanReview { get; set; }

    [Name("Multibidder Situation")]
    public string? MultibidderSituation { get; set; }

    [Name("New business growth: adjacency")]
    public bool? NewBusinessGrowthAdjacency { get; set; }

    [Name("New business growth: international")]
    public bool? NewBusinessGrowthInternational { get; set; }

    [Name("NPS")]
    public bool? NPS { get; set; }

    [Name("OperatingPartnerEcode")]
    [TypeConverter(typeof(ListConverter))]
    public IReadOnlyList<string> OperatingPartnerEcodes { get; set; }

    [Name("Ops DD Team")]
    public string? OpsDdTeam { get; set; }

    [TypeConverter(typeof(SpListConverter))]
    [Name("Ops/Margin Improve Details")]
    public IReadOnlyList<string>? OpsMarginImproveDetails { get; set; }

    [Name("Ops/Margin Improvement?")]
    public string? OpsMarginImprovement { get; set; }

    [Name("Organization design: overall & functional")]
    public bool? OrganizationDesignOverallFunctional { get; set; }

    [Name("Other")]
    public string? Other { get; set; }

    [Name("Primary research output satisfaction")]
    public short? PrimaryResearchOutputSatisfaction { get; set; }

    [Name("Primary Research Vendor1")]
    public string? PrimaryResearchVendor1 { get; set; }

    [Name("Primary Research Vendor2")]
    public string? PrimaryResearchVendor2 { get; set; }

    [Name("Primary Research Vendor3")]
    public string? PrimaryResearchVendor3 { get; set; }

    [Name("Primary Research?")]
    [TypeConverter(typeof(SpListConverter))]
    public IReadOnlyList<string> PrimaryResearch { get; set; }

    [Name("Proposal Collected")]
    public string? ProposalCollected { get; set; }

    [Name("RCP")]
    public bool? RCP { get; set; }

    [Name("ROS/ RMS")]
    public bool? ROSRMS { get; set; }

    [Name("Sanitised Client Name")]
    public string? SanitisedClientName { get; set; }

    [Name("Sector Insights")]
    public string? SectorInsights { get; set; }

    [Name("SourceFolder")]
    public string? SourceFolder { get; set; }

    [Name("Survey Instruments")]
    public bool? SurveyInstruments { get; set; }

    [Name("Survey Usage")]
    public bool? SurveyUsage { get; set; }

    [Name("Target Private Public")]
    public string? TargetPrivatePublic { get; set; }

    [Name("Title of market overviews (posted on GXC)")]
    public string? TitleOfMarketOverviewsPostedOnGXC { get; set; }

    [Name("VDD?")]
    public bool? VDd { get; set; }

    [Name("0-CreateFolder")]
    public string? CreateFolder { get; set; }

    [Name("2-RemoveCMPermission")]
    public string? RemoveCMPermission { get; set; }

    [Name("A1 VCP Inclusion")]
    public bool? A1VcpInclusion { get; set; }

    [Name("APAC Reminder 1")]
    public DateTime? ApacReminder1 { get; set; }

    [Name("APAC Reminder 2")]
    public DateTime? ApacReminder2 { get; set; }

    [Name("APAC Reminder 3")]
    public DateTime? ApacReminder3 { get; set; }

    [Name("App Created By")]
    public string? AppCreatedBy { get; set; }

    [Name("App Modified By")]
    public string? AppModifiedBy { get; set; }

    [Name("Bain Expert Groups / Ecosystem Partner usage ?")]
    [TypeConverter(typeof(SpListConverter))]
    public IReadOnlyList<string> BainExpertGroupsEcosystemPartnerUsage { get; set; }

    [Name("error")]
    public string? Error { get; set; }

    [Name("FN_BP")]
    public string? FN_BP { get; set; }

    [Name("FN_CM")]
    public string? FN_CM { get; set; }

    [Name("FN_KM")]
    public string? FN_KM { get; set; }

    [Name("Highlighted Case Aspects")]
    public string? HighlightedCaseAspects { get; set; }

    [Name("ID")]
    public string? ID { get; set; }

    [Name("ItemStage")]
    [TypeConverter(typeof(StringEnumConverter<MigrationCaseState>))]
    public MigrationCaseState? ItemStage { get; set; }

    [Name("VendorNPS1")]
    public string? VendorNps1 { get; set; }

    [Name("VendorNPS2")]
    public string? VendorNps2 { get; set; }

    [Name("VendorNPS3")]
    public string? VendorNps3 { get; set; }

    //[Name("DuplicateRecordForMigration")]
    //public string? DuplicateRecordForMigration { get; set; }

    [LineNumber]
    public int LineNumber { get; set; }
}

public class LineNumber : Attribute, IMemberMapper, IParameterMapper
{
    public void ApplyTo(MemberMap memberMap)
    {
        memberMap.Data.ReadingConvertExpression = (ConvertFromStringArgs args) => args.Row.Parser.Row;
    }

    public void ApplyTo(ParameterMap parameterMap)
    {
    }
}
#pragma warning restore CS8618