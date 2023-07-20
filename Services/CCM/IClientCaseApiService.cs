using PEXC.Case.Services.CCM.Contracts;

namespace PEXC.Case.Services.CCM;

public interface IClientCaseApiService
{
    Task<IReadOnlyCollection<CaseDetailsDto>> GetAllCasesModifiedAfter(DateOnly modifiedAfter, bool includeConfidential = true);
    Task<IReadOnlyCollection<CaseDetailsDto>> GetCasesByCaseCodes(IReadOnlyCollection<string> caseCodes, bool includeConfidential = true);
}