using PEXC.Case.DataContracts.V1;
using PEXC.Case.DataContracts.V1.CaseForms;
using PEXC.Case.Domain;
using PEXC.Case.Services.CCM.Contracts;
using PEXC.Case.Services.Mapping.EmployeeProfile;
using PEXC.Case.Services.Mapping.Taxonomy;
using static PEXC.Case.Services.Workflow.CaseDocumentHelper;

namespace PEXC.Case.Services.Mapping;

public class MainProfile : AutoMapper.Profile
{
    public const string MigrationUserDisplayName = "Migration Service";

    public const string CoveoFieldSeparator = ";";

    public MainProfile()
    {
        CaseEditProfile();
        SurveyProfile();
        CoveoProfile();
        CCMProfile();
    }

    private void CaseEditProfile()
    {
        CreateMap<CaseCreateDto, CaseEntity>()
            .IncludeMembers(t => t.CaseDetailsSection, t => t.TargetDetailsSection)
            .ConstructUsing(o => new CaseEntity(Guid.NewGuid().ToString(), o.CaseCode, RelationshipType.Retainer))
            .ForMember(c => c.CaseCode, o => o.Ignore()) // CaseCode is passed in the constructor where we have the logic responsible for removing whitespaces
            .ForMember(c => c.ItemStage, o => o.MapFrom(_ => CaseState.New))
            .ForMember(c => c.Created, o => o.MapFrom(_ => DateTime.UtcNow))
            .ForMember(c => c.Modified, o => o.MapFrom(_ => DateTime.UtcNow))
            .ForMember(e => e.UniqueId,
                o => o.MapFrom(
                    e => GenerateUniqueId(e.CaseCode, e.CaseDetailsSection!.CaseName!, RelationshipType.Retainer)));

        CreateMap<CaseEditDto, CaseEntity>()
            .IncludeMembers(t => t.CaseDetailsSection, t => t.TargetDetailsSection)
            .ReverseMap()
            .ForCtorParam(
                nameof(CaseEditDto.Published),
                opt => opt.MapFrom(
                    src => src.ItemStage == CaseState.Published
                        ? true
                        : src.ItemStage == CaseState.SurveyClosed
                            ? false
                            : default(bool?)))
            .AfterMap<CaseEditTaxonomyMapping>();

        CreateMap<TargetDetailsSectionDto, CaseEntity>()
            .IncludeMembers(
                t => t.TargetDetailsAndFinalDocumentsSection,
                t => t.UseOfExpertsSection,
                t => t.ScopeOfDiligenceSection,
                t => t.UseOfToolsSection)
            .ReverseMap();

        CreateMap<CaseDetailsSectionDto, CaseEntity>()
            .ReverseMap()
            .ForMember(c => c.PrimaryIndustry, o => o.Ignore())
            .ForMember(c => c.PrimaryCapability, o => o.Ignore())
            .ForMember(c => c.ManagingOffice, o => o.Ignore());

        CreateMap<CaseEntity, TargetDetailsAndFinalDocumentsSectionDto>()
            .ForMember(c => c.SecondaryIndustries, o => o.Ignore())
            .ForMember(
                dest => dest.CaseFolderUrl,
                opt => opt.MapFrom(src => MapCaseFolderUrl(src)))
            .ReverseMap();

        CreateMap<CaseEntity, UseOfExpertsSectionDto>()
            .ReverseMap();
        CreateMap<CaseEntity, ScopeOfDiligenceSectionDto>()
            .ReverseMap();
        CreateMap<CaseEntity, UseOfToolsSectionDto>()
            .ReverseMap();

        CreateMap<CaseEntity, CaseRequestInfoDto>()
            .ForMember(dst => dst.SharePointDirectoryDriveId, o => o.MapFrom(src => src.SharePointDirectory!.DriveId))
            .ForMember(dst => dst.SharePointDirectoryId, o => o.MapFrom(src => src.SharePointDirectory!.DirectoryId))
            .ForMember(dst => dst.SharePointDirectoryUrl, o => o.MapFrom(src => src.SharePointDirectory!.Url))
            .ForMember(dst => dst.Region, o => o.MapFrom(src => src.ManagingOffice!.Region))
            .ForMember(dst => dst.Year, o => o.MapFrom(src => MapYearBasedOnEndDate(src)))
            .AfterMap<ApplyIndustryMapping>();

        CreateMap<TaxonomyItemDto, TaxonomyItem>().ReverseMap();
        CreateMap<TaxonomyOfficeDto, TaxonomyOffice>().ReverseMap();
        CreateMap<ResearchRecommendationDto, ResearchRecommendation>().ReverseMap();
    }

    private void SurveyProfile()
    {
        CreateMap<SurveyDto, CaseEntity>()
            .ForMember(dst => dst.Key, o => o.Ignore())
            .IncludeMembers(t => t.SurveyCaseDetailsSection, t => t.SurveyTargetDetailsSection)
            .ReverseMap();

        CreateMap<SurveyTargetDetailsSectionDto, CaseEntity>()
            .IncludeMembers(t => t.TargetDetailsAndFinalDocumentsSection,
                t => t.UseOfExpertsSection,
                t => t.ScopeOfDiligenceSection,
                t => t.UseOfToolsSection)
            .ReverseMap();
        CreateMap<SurveyCaseDetailsSectionDto, CaseEntity>()
            .ReverseMap();
        CreateMap<CaseEntity, SurveyTargetDetailsAndFinalDocumentsSectionDto>()
            .ForMember(
                dest => dest.CaseFolderUrl,
                opt => opt.MapFrom(src => MapCaseFolderUrl(src)))
            .ReverseMap();
    }

    private void CoveoProfile()
    {
        CreateMap<CaseEntity, CaseSearchItemDto>()
            .IncludeMembers(t => t.HistoricFields)
            .ForMember(
                dest => dest.AdvancedAnalyticsUsage,
                opt => opt.MapFrom(src => JoinList(src.AdvancedAnalyticsUsage)))
            .AfterMap<CoveoItemTaxonomyMapping<CaseSearchItemDto>>()
            .AfterMap<CoveoItemEmployeeProfileMapping<CaseSearchItemDto>>()
            .AfterMap<CaseSearchItemConfidentialDataMapping>();

        CreateMap<CaseHistoricFieldsEntity, CaseSearchItemDto>();

        CreateMap<CaseEntity, CaseManagementItemDto>()
            .IncludeMembers(t => t.HistoricFields)
            .ForMember(
                dest => dest.CaseFolderUrl,
                opt => opt.MapFrom(src => MapCaseFolderUrl(src)))
            .ForMember(
                dest => dest.AdvancedAnalyticsUsage,
                opt => opt.MapFrom(src => JoinList(src.AdvancedAnalyticsUsage)))
            .ForMember(
                dest => dest.BainExpertGroupsEcosystemPartnerUsage,
                opt => opt.MapFrom(src => JoinList(src.BainExpertGroupsEcosystemPartnerUsage)))
            .ForMember(
                dest => dest.PrimaryResearch,
                opt => opt.MapFrom(src => JoinList(src.PrimaryResearch)))
            .ForMember(
                dest => dest.OpsMarginImprovementDetails,
                opt => opt.MapFrom(src => JoinList(src.OpsMarginImprovementDetails)))
            .ForMember(
                dest => dest.PrimaryResearchVendor1,
                opt => opt.MapFrom(src => MapPrimaryResearchVendor(src.PrimaryResearchRecommendation1)))
            .ForMember(
                dest => dest.VendorNps1,
                opt => opt.MapFrom(src => MapVendorNps(src.PrimaryResearchRecommendation1)))
            .ForMember(
                dest => dest.PrimaryResearchVendor2,
                opt => opt.MapFrom(src => MapPrimaryResearchVendor(src.PrimaryResearchRecommendation2)))
            .ForMember(
                dest => dest.VendorNps2,
                opt => opt.MapFrom(src => MapVendorNps(src.PrimaryResearchRecommendation2)))
            .ForMember(
                dest => dest.PrimaryResearchVendor3,
                opt => opt.MapFrom(src => MapPrimaryResearchVendor(src.PrimaryResearchRecommendation3)))
            .ForMember(
                dest => dest.VendorNps3,
                opt => opt.MapFrom(src => MapVendorNps(src.PrimaryResearchRecommendation3)))
            .AfterMap<CoveoItemTaxonomyMapping<CaseManagementItemDto>>()
            .AfterMap<CaseManagementItemEmployeeProfileMapping>();

        CreateMap<CaseHistoricFieldsEntity, CaseManagementItemDto>();

        CreateMap(typeof(PagedResult<>), typeof(PagedResult<>));
    }

    private void CCMProfile()
    {
        CreateMap<CaseDetailsDto, CaseEntity>()
            .ConstructUsing(cd => new CaseEntity(Guid.NewGuid().ToString(), cd.CaseCode, RelationshipType.NonRetainer))
            .ForMember(
                dest => dest.ClientHeadEcode,
                opt => opt.MapFrom(src => src.GlobalCoordinatingPartner))
            .ForMember(
                dest => dest.ManagerEcode,
                opt => opt.MapFrom(src => src.CaseManager))
            .ForMember(
                dest => dest.BillingPartnerEcode,
                opt => opt.MapFrom(src => src.BillingPartner))
            .ForMember(
                dest => dest.StartDate,
                opt => opt.MapFrom(src => src.StartDate == default(DateTime) ? null : src.StartDate))
            .ForMember(
                dest => dest.EndDate,
                opt => opt.MapFrom(src => src.EndDate == default(DateTime) ? null : src.EndDate))
            .ForMember(
                dest => dest.UniqueId,
                opt => opt.MapFrom(
                    src => GenerateUniqueId(src.CaseCode, src.CaseName!, RelationshipType.NonRetainer)))
            .ForMember(dest => dest.CaseCode, opt => opt.Ignore()) // CaseCode is passed in the constructor where we have the logic responsible for removing whitespaces
            .AfterMap<CcmTaxonomyMapping>();
    }

    private static string? MapPrimaryResearchVendor(ResearchRecommendation? researchRecommendation) => researchRecommendation?.Name;

    private static string? MapVendorNps(ResearchRecommendation? researchRecommendation) => researchRecommendation?.Rating.ToString();

    private static string? JoinList(IReadOnlyCollection<string>? valueList) =>
        valueList != null && valueList.Any() ? string.Join(CoveoFieldSeparator, valueList) : null;

    private static string? MapCaseFolderUrl(CaseEntity entity) =>
        entity.SharePointDirectory?.Url;

    private static string? MapYearBasedOnEndDate(CaseEntity entity) =>
        entity.EndDate?.Year.ToString();
}