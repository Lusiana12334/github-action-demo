using PEXC.Case.Services.IRIS.Contracts;

namespace PEXC.Case.Services.IRIS;

internal class IrisIntegrationService : IIrisIntegrationService
{
    private readonly IIrisApiService _service;

    public IrisIntegrationService(IIrisApiService service)
    {
        _service = service;
    }

    public async Task<IList<IrisCaseDto>> GetCasesByCaseCodes(IReadOnlyList<string> caseCodes)
    {
        var cases = new List<IrisCaseDto>();
        var batches = caseCodes.Chunk(300);
        await Parallel.ForEachAsync(batches, async (batch, _) =>
        {
            var caseCodesFilter = batch.ToList();
            var filter = new SearchCasesDto(null, caseCodesFilter, null, null);
            var results = await GetCases(filter);
            cases.AddRange(results);
        });
        return cases;
    }

    public async Task<IList<IrisCaseDto>> GetCasesModifiedAfter(
        DateOnly modifiedAfter,
        int[] pegIndustries,
        int[] pegCapabilities)
    {
        var filter = new SearchCasesDto(modifiedAfter, null, pegIndustries, pegCapabilities);
        var cases = await GetCases(filter);
        return cases;
    }

    private async Task<IList<IrisCaseDto>> GetCases(SearchCasesDto filter)
    {
        var cases = new List<IrisCaseDto>();
        ApiResult<IrisCaseDto> result;
        var pageNumber = 1;
        var pageCount = 1000;

        do
        {
            result = await _service.GetCases(pageNumber, pageCount, filter);
            cases.AddRange(result.Items);
            pageNumber++;
        } while (result.TotalCount > cases.Count && result.Items.Any());

        return cases;
    }
}