using System.Net.Http.Json;
using System.Web;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PEXC.Case.Services.CCM.Contracts;

namespace PEXC.Case.Tools.Migration.Ccm;

public interface ICcmApi
{
    Task<CaseDetailsDto[]> GetCases(IEnumerable<string> caseCodes);
}

public class CcmApi : ICcmApi
{
    private readonly HttpClient _client;

    private ILogger<CcmApi> _logger;

    public CcmApi(HttpClient client, ILogger<CcmApi> logger)
        => (_client, _logger) = (client, logger);

    public Task<CaseDetailsDto[]> GetCases(IEnumerable<string> caseCodes)
    {
        var codes = string.Join(",", caseCodes.Select(RemoveSpecialCharacters).Select(HttpUtility.UrlEncode));
        return _client.GetFromJsonAsync<CaseDetailsDto[]>(
            $"CaseDetails/getcasedetailsbycodes?caseCodes={codes}&includeConfidential=true")!;
    }

    private string RemoveSpecialCharacters(string code)
    {
        var newCode =  code.Replace("&", string.Empty)
            .Replace("/", string.Empty)
            .Replace("(", string.Empty)
            .Replace(")", string.Empty)
            .Replace(" ", string.Empty);

        if (newCode != code)
        {
            _logger.LogInformation("Change code {caseCode} to {newCode}", code, newCode);
        }

        return newCode;
    }
}

public class CmmCacheFileApi : ICcmApi
{
    private const string CacheFileName = "ccm_cache.json";

    private readonly ICcmApi _innerApi;

    private IDictionary<string, CaseDetailsDto>? _cases;

    private JsonWriter? _cacheWriter;

    private readonly JsonSerializer _serializer = new JsonSerializer();

    public CmmCacheFileApi(ICcmApi innerApi)
    {
        _innerApi = innerApi;
    }

    public async Task<CaseDetailsDto[]> GetCases(IEnumerable<string> caseCodes)
    {
        _cases ??= LoadCases();
        _cacheWriter ??= new JsonTextWriter(File.AppendText(CacheFileName));

        List<CaseDetailsDto> result = new List<CaseDetailsDto>();
        List<string> toLoad = new List<string>();

        foreach (var caseCode in caseCodes.Select(c => c.Trim()))
            if (_cases.TryGetValue(caseCode, out var details))
                result.Add(details);
            else
                toLoad.Add(caseCode);

        var items = toLoad.Count > 0
            ? await _innerApi.GetCases(toLoad)
            : Array.Empty<CaseDetailsDto>();

        foreach (var item in items)
        {
            _cases.Add(item.CaseCode, item);
            _serializer.Serialize(_cacheWriter, item);
            _cacheWriter.WriteWhitespace("\r\n");
            result.Add(item);
        }

        await _cacheWriter.FlushAsync();

        return result.ToArray();
    }

    private IDictionary<string, CaseDetailsDto> LoadCases()
    {
        if (!File.Exists(CacheFileName))
            return new Dictionary<string, CaseDetailsDto>();

        using var streamReader = File.OpenText(CacheFileName);
        var jsonReader = new JsonTextReader(streamReader) { SupportMultipleContent = true };
        var result = new List<CaseDetailsDto>();

        while (jsonReader.Read())
            result.Add(_serializer.Deserialize<CaseDetailsDto>(jsonReader)!);

        return result.ToDictionary(c => c.CaseCode);
    }
}

