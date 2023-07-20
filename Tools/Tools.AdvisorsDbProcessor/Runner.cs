using PEXC.Case.Services.Staffing;

namespace PEXC.Case.Tools.AdvisorsDbProcessor;

public class Runner
{
    private readonly IStaffingApiService _staffingApiService;

    private readonly DbFacade _dbFacade;

    public Runner(IStaffingApiService staffingApiService, DbFacade dbFacade)
    {
        _staffingApiService = new CachedStaffingApiService(staffingApiService);
        _dbFacade = dbFacade;
    }

    public async Task Run(Action<string> logger)
    {
        var items = await _dbFacade.LoadCaseCodes();

        logger($"Number of records to process: {items.Count}");

        decimal percent = 0;
        int processed = 0;
        foreach (var chunk in items.Chunk(10))
        {
            try
            {
                var codes = chunk.Select(i => i.CaseCode).ToList();
                logger($"Processing {string.Join(", ", codes)}");
                DateTime start = DateTime.UtcNow;
                var result = await _staffingApiService.GetCasesTeamMembers(codes);

                foreach (var caseInfo in chunk)
                    caseInfo.Advisors = result.GetValueOrDefault(caseInfo.Key)?.Advisors;

                await _dbFacade.PersistRecords(chunk);

                logger($"Done in {DateTime.UtcNow - start}");

                processed += 10;
                percent = 100 * (decimal)processed / items.Count;
                if (processed % 100 == 0)
                    logger($"Processed {processed} records, Percent: {percent:#.##}");
            }
            catch (Exception e)
            {
                var caseCodes = string.Join(", ", chunk.Select(i => i.CaseCode));
                logger($"Error during loading Staffing API for {caseCodes}: {e}\r\n");
            }
        }
    }
}