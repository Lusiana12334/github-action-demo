using PEXC.Case.Domain;
using PEXC.Case.Services.CCM.Contracts;
using PEXC.Case.Tools.Migration.Ccm;

namespace PEXC.Case.Tools.Migration.Transformations;

public class CcmLoader
{
    public MigrationContext Context { get; }

    private readonly ICcmApi _ccmApi;

    public CcmLoader(ICcmApi ccmApi, MigrationContext context)
    {
        Context = context;
        _ccmApi = ccmApi;
    }

    public async Task<IEnumerable<MigrationData>> LoadCcmApi(MigrationData[] buffer)
    {
        var nonRetainers =
            buffer.Where(b => b.LeapRecord.RelationshipType == RelationshipType.NonRetainer).ToList();

        var caseCodes = nonRetainers
            .Select(r => r.LeapRecord.CaseCode)
            .Distinct()
            .ToList();

        if (caseCodes.Count == 0)
            return buffer;

        try
        {
            var cases = (await LoadCcmCases(caseCodes, nonRetainers)).ToDictionary(c => c.CaseCode);

            foreach (var record in nonRetainers)
                record.CcmData = cases.TryGetValue(record.LeapRecord.CaseCode, out var ccmData) ? ccmData : null;
        }
        catch (Exception e)
        {
            foreach (var migrationData in nonRetainers)
                Context.AddException(migrationData.LeapRecord, e);

            return buffer.Where(b => b.LeapRecord.RelationshipType == RelationshipType.Retainer).ToList();
        }

        return buffer;
    }

    private async Task<CaseDetailsDto[]> LoadCcmCases(List<string> caseCodes, List<MigrationData> nonRetainers)
    {
        try
        {
            return await _ccmApi.GetCases(caseCodes);
        }
        catch (HttpRequestException)
        {
            return await LoadItemsOneByOne(nonRetainers, caseCodes);
        }
    }

    private async Task<CaseDetailsDto[]> LoadItemsOneByOne(List<MigrationData> cases, List<string> toLoad)
    {
        List<CaseDetailsDto> result = new List<CaseDetailsDto>();
        foreach (var caseCode in toLoad)
        {
            try
            {
                result.AddRange(await _ccmApi.GetCases(new[] { caseCode }));
            }
            catch (Exception e)
            {
                var @case = cases.Find(c => c.LeapRecord.CaseCode == caseCode);
                Context.AddException(@case!.LeapRecord, e);
            }
        }

        return result.ToArray();
    }
}