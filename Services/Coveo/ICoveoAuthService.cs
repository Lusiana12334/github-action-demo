using PEXC.Case.DataContracts.V1;

namespace PEXC.Case.Services.Coveo;

public interface ICoveoAuthService
{
    Task<AuthDataDto> GetCaseSearchAuthData();
    Task<AuthDataDto> GetCaseManagementAuthData();
}