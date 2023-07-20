namespace PEXC.Case.Services.Staffing;

public interface IStaffingApiService
{
    Task<IReadOnlyDictionary<string, CaseTeamMembers>> GetCasesTeamMembers(IReadOnlyCollection<string> caseCodes);
}