using PEXC.Case.DataContracts.V1;
using PEXC.Case.DataContracts.V1.CaseForms;

namespace PEXC.Case.Services;

public interface ICaseService
{
    Task<CaseEditDto> GetCase(string id, string key);
    Task<CaseIdentifierDto> AddCase(CaseCreateDto caseCreateDto);
    Task<bool> UpdateCase(CaseEditDto updateCaseDto);
    Task<bool> DeleteCase(string id, string key);
    Task<bool> IsCaseUnique(string caseCode, string caseName, string? currentCaseId = null);
}