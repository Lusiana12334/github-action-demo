using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PEXC.Case.DataContracts.V1;
using PEXC.Case.Services.Coveo;
using PEXC.Case.Services.Infrastructure;
using PEXC.Case.Tests.Common;
using PEXC.Common.BaseApi.User;

namespace PEXC.Case.Services.Tests.Coveo;

public class CoveoRefreshServiceTests
{
    private static readonly CoveoApiOptions CoveoApiOptions = new()
    {
        OrganizationId = "ORG_ID123",
        CaseSearchApiKey = "SearchToken123",
        CaseSearchSourceId = "CaseSearchId",
        CaseManagementSourceId = "CaseManagementId"
    };

    [Fact]
    public async Task RefreshCaseSearchIndex_WhenSucceeded_NoExceptionThrown()
    {
        // Arrange
        var repo = CreateCoveoRefreshService(
            MockHttpClient(
                request => IsRefreshRequestValid(
                    request,
                    CoveoApiOptions.CaseSearchRefreshApiKey,
                    CoveoApiOptions.CaseSearchSourceId)));

        // Act
        // Assert
        await repo.RefreshCaseSearchIndex();
    }

    [Fact]
    public async Task RefreshCaseSearchIndex_WhenFailedWith412_NoExceptionThrown()
    {
        // Arrange
        var repo = CreateCoveoRefreshService(
            MockHttpClient(
                request => IsRefreshRequestValid(
                    request,
                    CoveoApiOptions.CaseSearchRefreshApiKey,
                    CoveoApiOptions.CaseSearchSourceId),
                new RefreshIndexResponse("Some_Error_Code", "Error message"),
                HttpStatusCode.PreconditionFailed));

        // Act
        // Assert
        await repo.RefreshCaseSearchIndex();
    }

    [Fact]
    public async Task RefreshCaseSearchIndex_WhenFailedWithUnexpectedError_ThrowsException()
    {
        // Arrange
        var repo = CreateCoveoRefreshService(
            MockHttpClient<object>(
                request => IsRefreshRequestValid(
                    request,
                    CoveoApiOptions.CaseSearchRefreshApiKey,
                    CoveoApiOptions.CaseSearchSourceId),
                null,
                HttpStatusCode.Unauthorized));

        // Act
        var act = async () => await repo.RefreshCaseSearchIndex();

        // Assert
        await act
            .Should()
            .ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task RefreshCaseManagementIndex_WhenSucceeded_NoExceptionThrown()
    {
        // Arrange
        var repo = CreateCoveoRefreshService(
            MockHttpClient(
                request => IsRefreshRequestValid(
                    request,
                    CoveoApiOptions.CaseManagementRefreshApiKey,
                    CoveoApiOptions.CaseManagementSourceId)));

        // Act
        // Assert
        await repo.RefreshCaseManagementIndex();
    }

    [Fact]
    public async Task RefreshCaseManagementIndex_WhenFailedWith412_NoExceptionThrown()
    {
        // Arrange
        var repo = CreateCoveoRefreshService(
            MockHttpClient(
                request => IsRefreshRequestValid(
                    request,
                    CoveoApiOptions.CaseManagementRefreshApiKey,
                    CoveoApiOptions.CaseManagementSourceId),
                new RefreshIndexResponse("Some_Error_Code", "Error message"),
                HttpStatusCode.PreconditionFailed));

        // Act
        // Assert
        await repo.RefreshCaseManagementIndex();
    }

    [Fact]
    public async Task RefreshCaseManagementIndex_WhenFailedWithUnexpectedError_ThrowsException()
    {
        // Arrange
        var repo = CreateCoveoRefreshService(
            MockHttpClient<object>(
                request => IsRefreshRequestValid(
                    request,
                    CoveoApiOptions.CaseManagementRefreshApiKey,
                    CoveoApiOptions.CaseManagementSourceId),
                null,
                HttpStatusCode.Unauthorized));

        // Act
        var act = async () => await repo.RefreshCaseManagementIndex();

        // Assert
        await act
            .Should()
            .ThrowAsync<HttpRequestException>();
    }
    
    private static bool IsRefreshRequestValid(HttpRequestMessage r, string expectedApiKey, string expectedSourceId)
        => r.RequestUri!.PathAndQuery == $"/organizations/{CoveoApiOptions.OrganizationId}/sources/{expectedSourceId}/incrementalRefresh" &&
           r.Headers.Authorization!.Scheme == "Bearer" &&
           r.Headers.Authorization!.Parameter == expectedApiKey;

    private static CoveoRefreshService CreateCoveoRefreshService(HttpClient client)
    {
        return new CoveoRefreshService(client, Options.Create(CoveoApiOptions), Substitute.For<ILogger<CoveoRefreshService>>());
    }

    private static HttpClient MockHttpClient(
        Predicate<HttpRequestMessage> requestPredicate,
        HttpStatusCode statusCode = HttpStatusCode.OK)
        => MockHttpClient<object>(requestPredicate, statusCode);

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