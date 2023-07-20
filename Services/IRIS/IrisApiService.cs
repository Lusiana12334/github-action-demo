using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PEXC.Case.Services.IRIS.Contracts;

namespace PEXC.Case.Services.IRIS;

internal class IrisApiService : IIrisApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<IrisApiService> _logger;
    private static readonly JsonSerializerOptions JsonSerializerOptions =
        new(JsonSerializerDefaults.Web);

    public IrisApiService(HttpClient httpClient, ILogger<IrisApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ApiResult<IrisCaseDto>> GetCases(int pageNumber, int pageCount, SearchCasesDto filter)
    {
        var queryString = GetRequestParameters(pageNumber, pageCount, filter);

        _logger
            .LogInformation("Querying IRIS Integration service with parameters:{queryString}",
                queryString);

        var response = await _httpClient
            .GetFromJsonAsync<ApiResult<IrisCaseDto>>(
                $"api/integration/cases{queryString}",
                JsonSerializerOptions);

        _logger
            .LogInformation("The data was fetched from IRIS Integration service. Data:{response}",
                JsonSerializer.Serialize(response));

        return response!;
    }

    private string GetRequestParameters(int pageNumber, int pageCount, SearchCasesDto filter)
    {
        var primaryIndustries = filter.PrimaryIndustries != null ? string.Join(",", filter.PrimaryIndustries) : "";
        var primaryCapabilities = filter.PrimaryCapabilities != null ? string.Join(",", filter.PrimaryCapabilities) : "";
        var caseCodes = filter.CaseCodes != null ? string.Join(",", filter.CaseCodes) : "";
        var modifiedSince = filter.ModifiedSince?.ToString("yyyy-MM-dd");

        var queryString =
            $"?pageNumber={pageNumber}" +
            $"&pageCount={pageCount}" +
            $"&primaryIndustries={primaryIndustries}" +
            $"&primaryCapabilities={primaryCapabilities}" +
            $"&modifiedSince={modifiedSince}" +
            $"&caseCodes={caseCodes}";

        return queryString;
    }
}