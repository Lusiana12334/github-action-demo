using Microsoft.Extensions.Logging;
using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;
using PEXC.Common.BaseApi.Profile;
using PEXC.Common.BaseApi.Profile.DataContracts.V1;

namespace PEXC.Case.Services.Mapping.EmployeeProfile;

public class CaseManagementItemEmployeeProfileMapping : CoveoItemEmployeeProfileMapping<CaseManagementItemDto>
{
    public CaseManagementItemEmployeeProfileMapping(
        IProfileRepository profileRepository,
        ILogger<CaseManagementItemEmployeeProfileMapping> logger)
        : base(profileRepository, logger)
    {
    }

    protected override IEnumerable<string?> CollectEcodes(CaseEntity source)
    {
        var ecodes = new List<string?>(base.CollectEcodes(source));

        if (source.CreatedBy?.UserType == UserType.User)
            ecodes.Add(source.CreatedBy.UserEcode);
        if (source.BainExpertsEcodes != null)
            ecodes.AddRange(source.BainExpertsEcodes);

        ecodes.Add(source.LeadKnowledgeSpecialistEcode);
        ecodes.Add(source.ClientHeadEcode);

        return ecodes;
    }

    protected override void MapEmployeeNames(
        IReadOnlyDictionary<string, EmployeeDetailsDto> profiles,
        CaseEntity source,
        CaseManagementItemDto destination)
    {
        base.MapEmployeeNames(profiles, source, destination);
        if (source.CreatedBy != null)
            destination.CreatedBy = GetDisplayName(profiles, source.CreatedBy);
        destination.BainExperts = GetFullName(profiles, source.BainExpertsEcodes?.ToArray());
        destination.KmContactName = GetFullName(profiles, source.LeadKnowledgeSpecialistEcode);
        destination.ClientHeadName = GetFullName(profiles, source.ClientHeadEcode);
    }
}