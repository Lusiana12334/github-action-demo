using System.Net.Http.Headers;
using Flurl;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PEXC.Case.DataContracts.V1;
using PEXC.Case.Infrastructure;
using PEXC.Case.Services.Infrastructure;
using PEXC.Common.BaseApi.User;

namespace PEXC.Case.Services.Coveo;

public class CoveoAuthService : ICoveoAuthService
{
    private readonly CoveoApiOptions _options;
    private readonly HttpClient _client;
    private readonly IUserProvider _userProvider;

    public CoveoAuthService(
        HttpClient client,
        IUserProvider userProvider,
        IOptions<CoveoApiOptions> options)
    {
        _options = options.Value;
        _client = client;
        _userProvider = userProvider;
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<AuthDataDto> GetCaseSearchAuthData()
    {
        var coveoToken = await GetCoveoToken(_options.CaseSearchApiKey);
        return new AuthDataDto(coveoToken, _options.OrganizationId);
    }

    public async Task<AuthDataDto> GetCaseManagementAuthData()
    {
        var coveoToken = await GetCoveoToken(_options.CaseManagementApiKey);
        return new AuthDataDto(coveoToken, _options.OrganizationId);
    }
    private async Task<string> GetCoveoToken(string apiKey)
    {
        var user = await GetUser();
        var request = new CoveoAuthorizeRequest(new[] { user });
        var response = await _client.SetBearer(apiKey)
            .PostAsync<CoveoAuthorizeRequest, CoveoAuthorizeResponse>(Url.Combine(_options.Endpoint, "search", "token"), request);
        return response?.Token ?? throw new InvalidOperationException("Coveo token cannot be null");
    }

    private async Task<UserId> GetUser()
    {
        var userEcode = await _userProvider.GetCurrentUserEcode();
        return new UserId(userEcode, _options.Provider);
    }

}