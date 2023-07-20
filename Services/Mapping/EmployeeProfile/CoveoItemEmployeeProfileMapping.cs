using Microsoft.Extensions.Logging;
using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;
using PEXC.Common.BaseApi.Profile;
using PEXC.Common.BaseApi.Profile.DataContracts.V1;

namespace PEXC.Case.Services.Mapping.EmployeeProfile;

public class CoveoItemEmployeeProfileMapping<TDestination> : EmployeeProfileMapping<CaseEntity, TDestination> where TDestination : CaseSearchItemDto
{
    public CoveoItemEmployeeProfileMapping(
        IProfileRepository profileRepository,
        ILogger<CoveoItemEmployeeProfileMapping<TDestination>> logger)
        : base(profileRepository, logger)
    { }

    protected override IEnumerable<string?> CollectEcodes(CaseEntity source)
    {
        var ecodes = new List<string?>();

        if (source.ModifiedBy?.UserType == UserType.User)
            ecodes.Add(source.ModifiedBy.UserEcode);
        if (source.OperatingPartnerEcodes != null)
            ecodes.AddRange(source.OperatingPartnerEcodes);
        if (source.AdvisorsEcodes != null)
            ecodes.AddRange(source.AdvisorsEcodes);

        ecodes.Add(source.BillingPartnerEcode);
        ecodes.Add(source.ManagerEcode);

        return ecodes;
    }

    protected override void MapEmployeeNames(
        IReadOnlyDictionary<string, EmployeeDetailsDto> profiles,
        CaseEntity source,
        TDestination destination)
    {
        if (source.ModifiedBy != null)
            destination.ModifiedBy = GetDisplayName(profiles, source.ModifiedBy);

        destination.OperatingPartnerName = GetFullName(profiles, source.OperatingPartnerEcodes?.ToArray());
        destination.AdvisorsNames = GetFullName(profiles, source.AdvisorsEcodes?.ToArray());
        destination.BillingPartnerName = GetFullName(profiles, source.BillingPartnerEcode);
        destination.ManagerName = GetFullName(profiles, source.ManagerEcode);
    }
}