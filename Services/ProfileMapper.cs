using PEXC.Common.BaseApi.Profile;
using PEXC.Common.BaseApi.Profile.DataContracts.V1;

namespace PEXC.Case.Services;

public class ProfileMapper : IProfileMapper
{
    private readonly IProfileRepository _profileRepository;

    public ProfileMapper(IProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task<IDictionary<string, EmployeeDetailsDto>> GetEmployeeProfiles(
        IReadOnlyList<string> eCodes, string correlationId)
        => (await _profileRepository.GetProfiles(eCodes, correlationId))
            .Where(e => !string.IsNullOrEmpty(e.Email))
            .ToDictionary(e => e.EmployeeCode);
}