using System.Linq.Expressions;
using AutoMapper;
using PEXC.Case.Domain;
using PEXC.Case.Services.Mapping;
using PEXC.Case.Services.Workflow;
using PEXC.Document.DataContracts.V1;

namespace PEXC.Case.Tools.Migration;

#pragma warning disable CS0618

public class MigrationProfile : Profile
{
    public static DateTime MigrationDate = DateTime.UtcNow;

    public MigrationProfile()
    {
        Expression<Func<CaseEntity, object?>>[] propertiesToIgnore = {
            dst => dst.Id, // New Case Id is generated, we copy old ids to MigrationId/MigrationUniqueId property
            dst => dst.PrimaryIndustry,
            dst => dst.PrimaryIndustry,
            dst => dst.PrimaryCapability,
            dst => dst.SecondaryIndustries,
            dst => dst.SecondaryCapabilities,
            dst => dst.ManagingOffice,
            dst => dst.IsDeleted,
            dst => dst.HistoricFields,
            dst => dst.SharePointDirectory,
            dst => dst.CorrelationId,
            dst => dst.ClientId,
            dst => dst.Permissions,
            dst => dst.Key,
            dst => dst.IsSearchable,
            dst => dst.IsInConfidentialGracePeriod,
            dst => dst.TargetPubliclyTraded,
            dst => dst.Sensitive,
            det => det.CaseCode, // CaseCode is passed in the constructor where we have the logic responsible for removing whitespaces.
        };

        Expression<Func<LeapMasterRecord, object?>>[] srcPropertiesToIgnore = {
            src => src.LineNumber,
            src => src.PrimaryResearchVendor1,
            src => src.PrimaryResearchVendor2,
            src => src.PrimaryResearchVendor3,
            src => src.VendorNps1,
            src => src.VendorNps2,
            src => src.VendorNps3,
            src => src.Region,
            src => src.CaseType,
            src => src.ClientType,
            src => src.AppCreatedBy,
            src => src.AppModifiedBy,
            src => src.Error,
        };

        CreateMap<LeapMasterRecord, CaseEntity>()
            .ConstructUsing(lm => new CaseEntity(Guid.NewGuid().ToString(), lm.CaseCode, lm.RelationshipType))
            .ForMember(
                dst => dst.CreatedBy,
                o => o.MapFrom(e =>
                    new UserInfo(UserType.Service, $"{MainProfile.MigrationUserDisplayName}, original: {e.CreatedBy}")))
            .ForMember(
                dst => dst.ModifiedBy,
                o => o.MapFrom(e =>
                    new UserInfo(UserType.Service, $"{MainProfile.MigrationUserDisplayName}, original: {e.CreatedBy}")))
            .ForMember(
                dst => dst.Modified,
                o => o.MapFrom(src => DateTime.UtcNow))
            .ForMember(
                dst => dst.PrimaryResearchRecommendation1,
                o => o.MapFrom(e => GetResearchRecommendation(e.PrimaryResearchVendor1, e.VendorNps1)))
            .ForMember(
                dst => dst.PrimaryResearchRecommendation2,
                o => o.MapFrom(e => GetResearchRecommendation(e.PrimaryResearchVendor2, e.VendorNps2)))
            .ForMember(
                dst => dst.PrimaryResearchRecommendation3,
                o => o.MapFrom(e => GetResearchRecommendation(e.PrimaryResearchVendor3, e.VendorNps3)))
            .ForMember(
                dst => dst.OneNotifications,
                o => o.MapFrom(e => e.Notifications))
            .ForMember(
                dst => dst.MainCompetitorsAnalyzed,
                o => o.MapFrom(e => e.MainCompetitorsAnalyzedAsPartOfDd))
            .ForMember(
                dst => dst.GeographicRegion,
                o => o.MapFrom(e => e.GeographicRegionAsPartOfDd))
            .ForMember(
                dst => dst.AagGuidanceOnVendor,
                o => o.MapFrom(e => e.AAGGuidanceOnVendorOrSurvey))
            .ForMember(
                dst => dst.OpsMarginImprovementDetails,
                o => o.MapFrom(e => e.OpsMarginImproveDetails))
            .ForMember(
                dst => dst.IndustrySectorsAnalyzed,
                o => o.MapFrom(e => e.IndustrySectorsAnalyzedAsPartOfDd))
            .ForMember(
                dst => dst.OpsDdDuration,
                o => o.MapFrom(e => e.DurationOfOpsDd))
            // For now we always take LeadKS from migration data, from field: KMContactEcode
            .ForMember(
                dst => dst.LeadKnowledgeSpecialistEcode,
                o => o.MapFrom(e => e.KMContactEcode))
            .ForMember(
                dst => dst.UniqueId,
                o => o.MapFrom(e => GenerateUniqueId(e)))
            .ForPath(e => e.HistoricFields!.AdvancedAnalyticsUsageDetails,
                o => o.MapFrom(x => x.AdvancedAnalyticsUsageDetails))
            .ForPath(
                dst => dst.HistoricFields!.MigrationUniqueId,
                o => o.MapFrom(e => e.UniqueID))
            .ForPath(
                dst => dst.HistoricFields!.MigrationId,
                o => o.MapFrom(e => e.ID))

            .IgnoreProperties(propertiesToIgnore)
            .IgnoreSourceProperties(srcPropertiesToIgnore)

            .Flatten<LeapMasterRecord, CaseEntity, CaseHistoricFieldsEntity?, string>(ce => ce.HistoricFields)
            .Flatten<LeapMasterRecord, CaseEntity, CaseHistoricFieldsEntity?, bool?>(ce => ce.HistoricFields)
            .Flatten<LeapMasterRecord, CaseEntity, CaseHistoricFieldsEntity?, DateTime?>(ce => ce.HistoricFields)

            .ValidateMemberList(MemberList.Source)
            .ValidateMemberList(MemberList.Destination)
            .AfterMap<AssignMigrationDate>();

        CreateMap<DirectoryInfoDto, SharePointDirectoryEntity>();
    }

    private static ResearchRecommendation? GetResearchRecommendation(string? primaryResearchVendor, string? vendorNps) =>
        string.IsNullOrEmpty(vendorNps)
            ? null
            : new ResearchRecommendation(primaryResearchVendor ?? "", Convert.ToInt16(vendorNps));

    private static string GenerateUniqueId(LeapMasterRecord lm) =>
        CaseDocumentHelper.GenerateUniqueId(lm.CaseCode, lm.CaseName!, lm.RelationshipType);
}

internal class AssignMigrationDate : IMappingAction<LeapMasterRecord, CaseEntity>
{
    public void Process(LeapMasterRecord source, CaseEntity destination, ResolutionContext context)
    {
        destination.HistoricFields!.MigrationDate = MigrationProfile.MigrationDate;
        // For some reason mapping entries for those fields do not work
        destination.HistoricFields!.MigrationUniqueId = source.UniqueID;
        destination.HistoricFields!.MigrationId = source.ID;
    }
}

public static class MappingExtensions
{
    public static IMappingExpression<TSource, TDestination> IgnoreProperties<TSource, TDestination>(
        this IMappingExpression<TSource, TDestination> map,
        IEnumerable<Expression<Func<TDestination, object?>>> propertiesToIgnore)
    {
        foreach (var propertyToIgnore in propertiesToIgnore)
            map.ForMember(propertyToIgnore, o => o.Ignore());

        return map;
    }

    public static IMappingExpression<TSource, TDestination> IgnoreSourceProperties<TSource, TDestination>(
        this IMappingExpression<TSource, TDestination> map,
        IEnumerable<Expression<Func<TSource, object?>>> propertiesToIgnore)
    {
        foreach (var propertyToIgnore in propertiesToIgnore)
            map.ForSourceMember(propertyToIgnore, o => o.DoNotValidate());

        return map;
    }

    public static IMappingExpression<TSource, TDestination> Flatten<TSource, TDestination, TDestinationMember, TPropType>(
        this IMappingExpression<TSource, TDestination> map,
        Expression<Func<TDestination, TDestinationMember>> dest)
    {
        var innerSourceProperties = typeof(TSource).GetProperties()
            .Where(sp => sp.CanRead && sp.PropertyType == typeof(TPropType))
            .Join(typeof(TDestinationMember).GetProperties().Where(dp => dp.CanWrite), sp => sp.Name, dp => dp.Name, (_, dp) => dp);

        foreach (var property in innerSourceProperties)
        {
            var innerProperty = Expression.Property(dest.Body, property);
            var mapFrom = Expression.Lambda<Func<TDestination, TPropType>>(innerProperty, dest.Parameters);

            var par = Expression.Parameter(typeof(TSource));
            var srcProp = Expression.Property(par, property.Name);
            var mapFromSrc = Expression.Lambda<Func<TSource, TPropType>>(srcProp, par);

            map.ForPath(mapFrom, c => c.MapFrom(mapFromSrc));
        }
        return map;
    }
}

#pragma warning restore CS0618