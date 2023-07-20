using PEXC.Case.Services.IRIS.Contracts;

namespace PEXC.Case.Services.IRIS;

public interface IIrisIntegrationService
{
    Task<IList<IrisCaseDto>> GetCasesByCaseCodes(IReadOnlyList<string> caseCodes);
    Task<IList<IrisCaseDto>> GetCasesModifiedAfter(DateOnly modifiedAfter, int[] pegIndustries, int[] pegCapabilities);
}