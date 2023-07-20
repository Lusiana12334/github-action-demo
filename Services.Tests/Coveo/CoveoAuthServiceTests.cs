using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PEXC.Case.DataContracts.V1;
using PEXC.Case.Services.Coveo;
using PEXC.Case.Services.Infrastructure;
using PEXC.Case.Tests.Common;
using PEXC.Common.BaseApi.User;

namespace PEXC.Case.Services.Tests.Coveo;

public class CoveoAuthServiceTests
{
    private static readonly CoveoApiOptions CoveoApiOptions = new()
    {
        OrganizationId = "ORG_ID123",
        CaseSearchApiKey = "SearchToken123",
        CaseSearchSourceId = "CaseSearchId",
        CaseManagementSourceId = "CaseManagementId"
    };

    private static readonly string UserEcode = "SomeName";


    [Fact]
    public async Task GetCaseSearchAuthData_ReturnsExpectedToken()
    {
        // Arrange
        var expectedResponse = new CoveoAuthorizeResponse { Token = "ABC123" };
        var repo = CreateCoveoRefreshService(
            MockHttpClient(
                r => IsAuthorizeRequestValid(r, CoveoApiOptions.CaseSearchApiKey).GetAwaiter().GetResult(),
                expectedResponse));

        // Act
        var result = await repo.GetCaseSearchAuthData();

        // Assert
        result.Token
            .Should()
            .Be(expectedResponse.Token);
        result.OrganizationId
            .Should()
            .Be(CoveoApiOptions.OrganizationId);
    }

    [Fact]
    public async Task GetCaseManagementAuthData_ReturnsExpectedToken()
    {
        // Arrange
        var expectedResponse = new CoveoAuthorizeResponse { Token = "ABC123" };
        var repo = CreateCoveoRefreshService(
            MockHttpClient(
                request => IsAuthorizeRequestValid(request, CoveoApiOptions.CaseManagementApiKey).GetAwaiter().GetResult(),
                expectedResponse));

        // Act
        var result = await repo.GetCaseManagementAuthData();

        // Assert
        result.Token
            .Should()
            .Be(expectedResponse.Token);
        result.OrganizationId
            .Should()
            .Be(CoveoApiOptions.OrganizationId);
    }

    private static CoveoAuthService CreateCoveoRefreshService(HttpClient client)
    {
        var userProvider = Substitute.For<IUserProvider>();
        userProvider.GetCurrentUserEcode().Returns(Task.FromResult(UserEcode));

        return new CoveoAuthService(client, userProvider, Options.Create(CoveoApiOptions));
    }

    private static async Task<bool> IsAuthorizeRequestValid(HttpRequestMessage r, string expectedApiKey)
        => r.RequestUri!.PathAndQuery == "/search/token" &&
           r.Headers.Authorization!.Scheme == "Bearer" &&
           r.Headers.Authorization!.Parameter == expectedApiKey &&
           (await r.Content!.ReadFromJsonAsync<CoveoAuthorizeRequest>())!.UserIds.Single().Name == UserEcode;
    
    private static HttpClient MockHttpClient<TResponse>(
        Predicate<HttpRequestMessage> requestPredicate,
        TResponse? expectedResponse = null,
        HttpStatusCode statusCode = HttpStatusCode.OK) where TResponse : class
        => new(
            expectedResponse != null
                ? new SimpleMockHttpMessageHandler(expectedResponse, statusCode, requestPredicate)
                : new SimpleMockHttpMessageHandler(new HttpResponseMessage(statusCode), requestPredicate))
        {
            BaseAddress = new Uri("https://some.base.address.com/api")
        };
}