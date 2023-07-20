using PEXC.Common.BaseApi.Profile.DataContracts.V1;

namespace PEXC.Case.Services;

public interface IProfileMapper
{
    Task<IDictionary<string, EmployeeDetailsDto>> GetEmployeeProfiles(IReadOnlyList<string> eCodes, string correlationId);
}