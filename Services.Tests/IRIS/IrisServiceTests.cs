using PEXC.Case.Services.IRIS.Contracts;
using PEXC.Case.Services.IRIS;
using PEXC.Case.Tests.Common;
using System.Net;
using Microsoft.Extensions.Logging;

namespace PEXC.Case.Services.Tests.IRIS;

public class IrisServiceTests
{
    [Fact]
    public async Task GetCases_CheckAllParameters_ReturnCases()
    {
        // Arrange
        var expectedItems = new ApiResult<IrisCaseDto>
        {
            Items = new List<IrisCaseDto>
            {
                new("1")
            },
            TotalCount = 1
        };

        var filter = new SearchCasesDto(
            new DateOnly(2022, 10, 15),
            new[] { "1" },
            new[] { 1, 2 },
            new[] { 3, 4 });

        HttpClient httpClient =
            MockHttpClient(
                r => r.RequestUri!.PathAndQuery ==
                     "/api/integration/cases?pageNumber=1&pageCount=10&primaryIndustries=1,2&primaryCapabilities=3,4&modifiedSince=2022-10-15&caseCodes=1",
                expectedItems);

        var logger = Substitute.For<ILogger<IrisApiService>>();

        // Act
        var service = new IrisApiService(httpClient, logger);
        var result = await service.GetCases(1, 10, filter);

        // Assert
        result.Should().BeEquivalentTo(expectedItems);
    }

    private static HttpClient MockHttpClient(
        Predicate<HttpRequestMessage> requestPredicate,
        ApiResult<IrisCaseDto> expectedResponse,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpClient(new SimpleMockHttpMessageHandler(expectedResponse, statusCode, requestPredicate))
        {
            BaseAddress = new Uri("https://baseUrl.com/api")
        };
    }
}