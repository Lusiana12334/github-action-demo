using System.Collections.ObjectModel;
using Newtonsoft.Json;
using PEXC.Case.Services.Staffing;

namespace PEXC.Case.Tools.AdvisorsDbProcessor;

public class CachedStaffingApiService : IStaffingApiService
{
    private const string CacheFileName = "staffing_api.json";

    private readonly IStaffingApiService _innerService;

    private readonly JsonSerializer _serializer = new JsonSerializer();

    private IDictionary<string, CaseTeamMembers>? _cases;

    private JsonWriter? _cacheWriter;

    private List<string> _emptyList = new List<string>();

    private static readonly IReadOnlyDictionary<string, CaseTeamMembers> Empty 
        = new ReadOnlyDictionary<string, CaseTeamMembers>(new Dictionary<string, CaseTeamMembers>());

    public CachedStaffingApiService(IStaffingApiService innerService) 
        => _innerService = innerService;

    public async Task<IReadOnlyDictionary<string, CaseTeamMembers>> GetCasesTeamMembers(IReadOnlyCollection<string> caseCodes)
    {
        _cases ??= LoadCases();
        _cacheWriter ??= new JsonTextWriter(File.AppendText(CacheFileName));

        var result = new Dictionary<string, CaseTeamMembers>();
        List<string> toLoad = new List<string>();

        foreach (var caseCode in caseCodes.Select(c => c.Trim()))
            if (_cases.TryGetValue(caseCode, out var teamMembers))
                result.Add(caseCode, teamMembers);
            else
                toLoad.Add(caseCode);

        var items = toLoad.Count > 0
                ? await _innerService.GetCasesTeamMembers(toLoad)
                : Empty;
        foreach (var key in toLoad)
        {
            var value = items.GetValueOrDefault(key) ?? new CaseTeamMembers(_emptyList, _emptyList);
            _cases.Add(key, value);
            result.Add(key, value);

            CacheItem(key, value);
        }

        await _cacheWriter.FlushAsync();

        return result;
    }

    private void CacheItem(string key, CaseTeamMembers value)
    {
        _serializer.Serialize(_cacheWriter!, new CaseTeamMembersWrapper(key, value));
        _cacheWriter!.WriteWhitespace("\r\n");
    }

    private IDictionary<string, CaseTeamMembers> LoadCases()
    {
        if (!File.Exists(CacheFileName))
            return new Dictionary<string, CaseTeamMembers>();

        using var streamReader = File.OpenText(CacheFileName);
        var jsonReader = new JsonTextReader(streamReader) { SupportMultipleContent = true };
        var result = new List<CaseTeamMembersWrapper>();

        while (jsonReader.Read())
            result.Add(_serializer.Deserialize<CaseTeamMembersWrapper>(jsonReader)!);

        return result.ToDictionary(c => c.CaseCode, c => c.Members);
    }
}

public record CaseTeamMembersWrapper(string CaseCode, CaseTeamMembers Members);