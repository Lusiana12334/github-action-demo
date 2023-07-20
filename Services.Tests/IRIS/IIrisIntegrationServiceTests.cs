using PEXC.Case.Services.IRIS;
using PEXC.Case.Services.IRIS.Contracts;

namespace PEXC.Case.Services.Tests.IRIS;

public class IrisIntegrationServiceTests
{
    [Fact]
    public async Task GetCasesByCaseCodes_ReturnCollection()
    {
        // Arrange
        var caseCodes = new[] { "code1" };
        var expectedResult = new ApiResult<IrisCaseDto>
        {
            TotalCount = 1,
            Items = new List<IrisCaseDto>
            {
                new("code1")
            }
        };

        var apiService = Substitute.For<IIrisApiService>();
        apiService
            .GetCases(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<SearchCasesDto>())
            .Returns(expectedResult);

        // Act
        var service = new IrisIntegrationService(apiService);
        var result = await service.GetCasesByCaseCodes(caseCodes);

        // Assert
        await apiService.Received().GetCases(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<SearchCasesDto>());
        result.Should().BeEquivalentTo(expectedResult.Items);
    }

    [Fact]
    public async Task GetCasesByCaseCodes_ExecuteInBatches()
    {
        // Arrange
        var caseCodes = Enumerable.Range(0, 350).Select(i => $"code{i}").ToList();
        var expectedResult = new ApiResult<IrisCaseDto>();

        var apiService = Substitute.For<IIrisApiService>();
        apiService
            .GetCases(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<SearchCasesDto>())
            .Returns(expectedResult);

        // Act
        var service = new IrisIntegrationService(apiService);
        await service.GetCasesByCaseCodes(caseCodes);

        // Assert
        await apiService.Received(2).GetCases(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<SearchCasesDto>());
    }

    [Fact]
    public async Task GetCasesModifiedAfter_ReturnCollection()
    {
        // Arrange
        var searchFilter = new SearchCasesDto(new DateOnly(2023, 1, 12), null, new[] { 1, 2, 3 }, new[] { 4, 5 });

        var expectedResult = new ApiResult<IrisCaseDto>
        {
            TotalCount = 1,
            Items = new List<IrisCaseDto>
            {
                new("code1")
            }
        };

        var apiService = Substitute.For<IIrisApiService>();
        apiService
            .GetCases(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Is<SearchCasesDto>(f =>
                    f.ModifiedSince.Equals(searchFilter.ModifiedSince)
                    && f.PrimaryIndustries!.SequenceEqual(searchFilter.PrimaryIndustries!)
                    && f.PrimaryCapabilities!.SequenceEqual(searchFilter.PrimaryCapabilities!)))
            .Returns(expectedResult);

        // Act
        var service = new IrisIntegrationService(apiService);
        var result = await service
            .GetCasesModifiedAfter(
                searchFilter.ModifiedSince!.Value,
                searchFilter.PrimaryIndustries!.ToArray(),
                searchFilter.PrimaryCapabilities!.ToArray());

        // Assert
        await apiService.Received().GetCases(
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Is<SearchCasesDto>(p =>
                p.ModifiedSince.Equals(searchFilter.ModifiedSince)
                && p.PrimaryIndustries!.SequenceEqual(searchFilter.PrimaryIndustries!)
                && p.PrimaryCapabilities!.SequenceEqual(searchFilter.PrimaryCapabilities!)));

        result.Should().BeEquivalentTo(expectedResult.Items);
    }
}