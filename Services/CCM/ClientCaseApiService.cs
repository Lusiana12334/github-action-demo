using System.Net.Http.Json;
using System.Web;
using PEXC.Case.Services.CCM.Contracts;

namespace PEXC.Case.Services.CCM;

internal class ClientCaseApiService : IClientCaseApiService
{
    private readonly HttpClient _httpClient;

    public ClientCaseApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyCollection<CaseDetailsDto>> GetAllCasesModifiedAfter(
        DateOnly modifiedAfter,
        bool includeConfidential = true)
    {
        return await _httpClient.GetFromJsonAsync<CaseDetailsDto[]>(
                @$"CaseDetails/GetAllCasesModifiedAfter?modifiedAfter={modifiedAfter:yyyy-MM-dd}&includeConfidential={includeConfidential}")
            ?? Array.Empty<CaseDetailsDto>();
    }

    public async Task<IReadOnlyCollection<CaseDetailsDto>> GetCasesByCaseCodes(
        IReadOnlyCollection<string> caseCodes,
        bool includeConfidential = true)
    {
        var caseCodesParam = HttpUtility.UrlEncode(string.Join(',', caseCodes));
        return await _httpClient.GetFromJsonAsync<CaseDetailsDto[]>(
               @$"CaseDetails/GetCaseDetailsByCodes?caseCodes={caseCodesParam}&includeConfidential={includeConfidential}")
           ?? Array.Empty<CaseDetailsDto>();
    }
}