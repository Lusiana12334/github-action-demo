namespace PEXC.Case.Services.Coveo;

public interface ICoveoRefreshService
{
    Task RefreshCaseSearchIndex();
    Task RefreshCaseManagementIndex();
}