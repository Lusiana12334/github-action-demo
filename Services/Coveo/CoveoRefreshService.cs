using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Flurl;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PEXC.Case.Infrastructure;
using PEXC.Case.Services.Infrastructure;

namespace PEXC.Case.Services.Coveo;

public class CoveoRefreshService : ICoveoRefreshService
{
    private readonly HttpClient _client;
    private readonly ILogger<CoveoRefreshService> _logger;
    private readonly CoveoApiOptions _options;

    public CoveoRefreshService(
        HttpClient client,
        IOptions<CoveoApiOptions> options,
        ILogger<CoveoRefreshService> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }
    
    private HttpClient GetClient(string apiKey)
        => _client.SetBearer(apiKey);

    public Task RefreshCaseSearchIndex()
        => RefreshIndex(_options.CaseSearchRefreshApiKey, _options.CaseSearchSourceId);

    public Task RefreshCaseManagementIndex()
        => RefreshIndex(_options.CaseManagementRefreshApiKey, _options.CaseManagementSourceId);

    private async Task RefreshIndex(string apiKey, string sourceId)
    {
        var response = await GetClient(apiKey)
            .PostAsync(
                Url.Combine(
                    _options.Endpoint,
                    "organizations",
                    _options.OrganizationId,
                    "sources",
                    sourceId,
                    "incrementalRefresh"),
                null);

        if (response.StatusCode is HttpStatusCode.PreconditionFailed)
        {
            var result = await response.Content.ReadFromJsonAsync<RefreshIndexResponse>() ?? new RefreshIndexResponse(null, null);
            _logger.LogWarning(
                "Refreshing the Coveo index failed with error: [{refreshErrorCode}]:[{refreshErrorMessage}]. Probably another refresh is already in progress. Ignoring the error.",
                result.ErrorCode,
                result.Message);
        }
        else
        {
            response.EnsureSuccessStatusCode();
        }
    }
}