namespace PEXC.Case.Services.CCM;

public interface ICaseDataImportService
{
    Task ImportCases();

    Task<(IReadOnlyCollection<string> UpdatedCaseCodes, IReadOnlyCollection<string> CreatedCaseCodes)>
        ImportCasesByCaseCodes(IReadOnlyCollection<string> caseCodes);
}