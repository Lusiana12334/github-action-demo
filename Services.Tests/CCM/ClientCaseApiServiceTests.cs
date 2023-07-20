using System.Net;
using PEXC.Case.Services.CCM;
using PEXC.Case.Services.CCM.Contracts;
using PEXC.Case.Tests.Common;

namespace PEXC.Case.Services.Tests.CCM;

public class ClientCaseApiServiceTests
{
    [Fact]
    public async Task GetAllCasesModifiedAfter_WhenDefaultIncludeConfidentialParameter_QuerySentWithIncludeConfidentialTrue()
    {
        // Arrange
        var expectedItems = new List<CaseDetailsDto>
        {
            new("1"), new("2"), new("3")
        };

        var httpClient =
            MockHttpClient(
                r => r.RequestUri!.PathAndQuery == "/CaseDetails/GetAllCasesModifiedAfter?modifiedAfter=2022-11-11&includeConfidential=True",
                expectedItems);

        // Act
        IClientCaseApiService service = new ClientCaseApiService(httpClient);
        var result = await service.GetAllCasesModifiedAfter(new DateOnly(2022, 11, 11));

        // Assert
        result.Should().BeEquivalentTo(expectedItems);
    }

    [Fact]
    public async Task GetAllCasesModifiedAfter_WhenIncludeConfidentialFalse_QuerySentWithIncludeConfidentialFalse()
    {
        // Arrange
        var expectedItems = new List<CaseDetailsDto>
        {
            new("1"), new("2"), new("3")
        };

        var httpClient =
            MockHttpClient(
                r => r.RequestUri!.PathAndQuery == "/CaseDetails/GetAllCasesModifiedAfter?modifiedAfter=2022-11-11&includeConfidential=False",
                expectedItems);

        // Act
        IClientCaseApiService service = new ClientCaseApiService(httpClient);
        var result = await service.GetAllCasesModifiedAfter(new DateOnly(2022, 11, 11), false);

        // Assert
        result.Should().BeEquivalentTo(expectedItems);
    }

    [Fact]
    public async Task GetAllCasesModifiedAfter_WhenStatusNotSuccess_ThrowsHttpRequestException()
    {
        // Arrange
        var expectedItems = new List<CaseDetailsDto>
        {
            new("1"), new("2"), new("3")
        };

        var httpClient =
            MockHttpClient(
                r => r.RequestUri!.PathAndQuery == "/CaseDetails/GetAllCasesModifiedAfter?modifiedAfter=2022-11-11&includeConfidential=False",
                expectedItems,
                HttpStatusCode.BadRequest);

        // Act
        // Assert
        IClientCaseApiService service = new ClientCaseApiService(httpClient);
        var call = () => service.GetAllCasesModifiedAfter(new DateOnly(2022, 11, 11), false);
        await call.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetCasesByCaseCodes_WhenDefaultIncludeConfidentialParameter_QuerySentWithIncludeConfidentialTrue()
    {
        // Arrange
        var expectedItems = new List<CaseDetailsDto>
        {
            new("1"), new("2"), new("3")
        };

        var httpClient =
            MockHttpClient(
                r => r.RequestUri!.PathAndQuery == "/CaseDetails/GetCaseDetailsByCodes?caseCodes=1%2c2%2c3&includeConfidential=True",
                expectedItems);

        // Act
        IClientCaseApiService service = new ClientCaseApiService(httpClient);
        var result = await service.GetCasesByCaseCodes(expectedItems.Select(i => i.CaseCode).ToArray());

        // Assert
        result.Should().BeEquivalentTo(expectedItems);
    }

    [Fact]
    public async Task GetCasesByCaseCodes_WhenIncludeConfidentialFalse_QuerySentWithIncludeConfidentialFalse()
    {
        // Arrange
        var expectedItems = new List<CaseDetailsDto>
        {
            new("1"), new("2"), new("3")
        };

        var httpClient =
            MockHttpClient(
                r => r.RequestUri!.PathAndQuery == "/CaseDetails/GetCaseDetailsByCodes?caseCodes=1%2c2%2c3&includeConfidential=False",
                expectedItems);

        // Act
        IClientCaseApiService service = new ClientCaseApiService(httpClient);
        var result = await service.GetCasesByCaseCodes(expectedItems.Select(i => i.CaseCode).ToArray(), false);

        // Assert
        result.Should().BeEquivalentTo(expectedItems);
    }

    [Fact]
    public async Task GetCasesByCaseCodes_WhenStatusNotSuccess_ThrowsHttpRequestException()
    {
        // Arrange
        var expectedItems = new List<CaseDetailsDto>
        {
            new("1"), new("2"), new("3")
        };

        var httpClient =
            MockHttpClient(
                r => r.RequestUri!.PathAndQuery == "/CaseDetails/GetCaseDetailsByCodes?caseCodes=1%2c2%2c3&includeConfidential=False",
                expectedItems,
                HttpStatusCode.BadRequest);

        // Act
        // Assert
        IClientCaseApiService service = new ClientCaseApiService(httpClient);
        var call = () => service.GetCasesByCaseCodes(expectedItems.Select(i => i.CaseCode).ToArray(), false);
        await call.Should().ThrowAsync<HttpRequestException>();
    }

    private static HttpClient MockHttpClient(
        Predicate<HttpRequestMessage> requestPredicate,
        IEnumerable<CaseDetailsDto> expectedResponse,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var httpMessageHandler = new SimpleMockHttpMessageHandler(expectedResponse, statusCode, requestPredicate);
        return new HttpClient(httpMessageHandler)
        {
            BaseAddress = new Uri("https://some.base.address.com/api")
        };
    }
}