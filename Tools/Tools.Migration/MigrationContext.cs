using System.Collections.Concurrent;
using System.Globalization;
using CsvHelper;
using PEXC.Case.Tools.Migration.Transformations;

namespace PEXC.Case.Tools.Migration;

public record MigrationContext
{
    private static readonly string[] Header = {
        "LineNumber","UniqueID",
        "Case Code","Case Name",
        "Error Code 1","Error Message 1",
        "Error Code 2","Error Message 2",
        "Error Code 3","Error Message 3"
    };

    private readonly object _lock = new object();

    private readonly List<Tuple<LeapMasterRecord, Exception>> _errors = new();

    private readonly List<LeapMasterRecord> _missingCcmCases = new();

    private readonly Dictionary<LeapMasterRecord, int> _duplicates = new(new LeapMasterComparer());

    private readonly Dictionary<string, Dictionary<string, List<LeapMasterRecord>>> _unmappedTerms = new();

    private readonly Dictionary<string, int> _unmappedStats = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<Guid, int> _unmappedTagIds = new();

    private readonly Dictionary<string, List<LeapMasterRecord>> _missingEcodes = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<LeapMasterRecord, TaxonomyDiscrepancy> _differentTaxonomies = new(LeapMasterComparer.Instance);

    private readonly ConcurrentDictionary<LeapMasterRecord, List<(string code, string message)>> _errorsByRecord =
        new(new LeapMasterComparer());

    private readonly List<LeapMasterRecord> _surveyReopenings = new();

    private readonly Dictionary<LeapMasterRecord, List<ECodeDiscrepancy>> _differentECodes = new(LeapMasterComparer.Instance);

    private readonly List<Tuple<LeapMasterRecord, bool, bool>> _archivedSurveys = new();

    private readonly List<MigrationData> _billingPartnerTerminated = new List<MigrationData>();

    public bool HasUnmappedStats => _unmappedStats.Count > 0;

    public bool HasUnmappedTagIds => _unmappedTagIds.Count > 0;

    public bool HasDuplicates => _duplicates.Count > 0;

    public bool HasBillingPartnerTerminated => _billingPartnerTerminated.Count > 0;

    public void AddMissingCcmRecord(LeapMasterRecord record)
    {
        lock (_lock)
            _missingCcmCases.Add(record);

        AddGenericError(record, "ccm_missing", "Record in CCM is missing");
    }

    public void AddDuplicate(LeapMasterRecord record)
    {
        if (!_duplicates.TryGetValue(record, out var count))
            AddGenericError(record, "duplicate", "Duplicate case");

        _duplicates[record] = count + 1;

    }

    public void AddUnmapped(LeapMasterRecord record, string propertyName, string textTerm)
    {
        lock (_lock)
        {
            if (!_unmappedTerms.TryGetValue(propertyName, out var propStats))
            {
                propStats = new Dictionary<string, List<LeapMasterRecord>>();
                _unmappedTerms.Add(propertyName, propStats);
            }

            if (!propStats.TryGetValue(textTerm, out var list))
            {
                list = new List<LeapMasterRecord>();
                propStats.Add(textTerm, list);
            }

            list.Add(record);

            var stat = _unmappedStats.GetValueOrDefault(textTerm);
            _unmappedStats[textTerm] = ++stat;
        }

        AddGenericError(record, "unmapped_taxonomy", $"Unmapped taxonomy in field '{propertyName}': '{textTerm}'");
    }

    public void AddException(LeapMasterRecord record, Exception e)
    {
        lock (_lock)
            _errors.Add(Tuple.Create(record, e));

        AddGenericError(record, "exception", $"Exception in processing: {e.Message}");
    }

    public void AddInvalidEcode(LeapMasterRecord record, string property, string ecode)
    {
        lock (_lock)
        {
            if (!_missingEcodes.TryGetValue(ecode, out var list))
            {
                list = new List<LeapMasterRecord>();
                _missingEcodes.Add(ecode, list);
            }

            list.Add(record);
        }

        AddGenericError(record, "invalid_ecode", $"ECode '{ecode}' does not exist in WorkDay in property '{property}'");
    }


    internal void AddMissingEcode(LeapMasterRecord record, string property)
    {
        AddGenericError(record, "missing_ecode", $"ECode is empty in property '{property}'");
    }

    internal void AddMultipleEcodes(LeapMasterRecord record, string property, string ecode)
    {
        AddGenericError(record, "multiple_ecodes", $"Property '{property}' contains multiple ecodes: '{ecode}'");
    }

    private void AddGenericError(LeapMasterRecord record, string code, string message)
    {
        var err = (code, message);
        _errorsByRecord.AddOrUpdate(record, r => new List<(string, string)> { err }, (_, value) =>
        {
            var copy = value.ToList();
            copy.Add(err);
            return copy;
        });
    }

    public void MissingTaxonomyTagId(LeapMasterRecord record, string property, Guid tagId)
    {
        AddGenericError(record, "invalid_tag_id", $"Property '{property}' has tag id that does not exist in Poolparty : '{tagId}'");
        var stat = _unmappedTagIds.GetValueOrDefault(tagId);
        _unmappedTagIds[tagId] = ++stat;
    }

    public void CheckAndReportDifferentEcode(
        LeapMasterRecord record,
        string property,
        string? ccmEcode,
        Dictionary<string, (string Name, bool IsTerminated)> names)
    {
        var leapECode = record.GetProperty(property);

        if (AreEqual(leapECode, ccmEcode))
            return;

        if (!_differentECodes.TryGetValue(record, out var list))
        {
            list = new List<ECodeDiscrepancy>();
            _differentECodes.Add(record, list);
        }

        var ccmName = names.GetValueOrDefault(ccmEcode!).Name;
        var leapName = names.GetValueOrDefault(leapECode).Name;
        list.Add(new ECodeDiscrepancy(property, $"{ccmEcode} ({ccmName})", $"{leapECode} ({leapName})"));
        AddGenericError(record, "different_ecode_in_ccm", $"Property '{property}' has different value in LeapMaster '{leapECode}' and in CCM: '{ccmEcode}'");
    }

    private bool AreEqual(string? a, string? b)
        => string.IsNullOrEmpty(a)
           || string.IsNullOrEmpty(b)
           || StringComparer.OrdinalIgnoreCase.Equals(a.Trim(), b.Trim());

    public void AddSurveyReopen(LeapMasterRecord record)
    {
        _surveyReopenings.Add(record);
    }

    public void AddDifferentTaxonomy(LeapMasterRecord record, string property, string leapName, string ccmName)
    {
        var value = _differentTaxonomies.GetValueOrDefault(record) ?? new TaxonomyDiscrepancy();
        if (property == nameof(LeapMasterRecord.ClientType))
        {
            value.CcmClient = ccmName;
            value.LeapClient = leapName;
        }
        else
        {
            value.CcmCase = ccmName;
            value.LeapCase = leapName;
        }

        _differentTaxonomies[record] = value;
    }

    public void AddSurveyArchive(LeapMasterRecord record, bool recordIsOld, bool caseManagedTerminated)
    {
        _archivedSurveys.Add(Tuple.Create(record, recordIsOld, caseManagedTerminated));
    }

    public void AddBillingPartnerTerminated(MigrationData record) => _billingPartnerTerminated.Add(record);

    public void WriteArchivedSurveys(TextWriter writer)
    {
        using var csvWriter = new CsvWriter(writer, CultureInfo.CurrentCulture);
        WriteHeader(csvWriter,
            new[] { "Case Code", "Case Name", "Modified", "Case End Date", "Case is old",
                    "Case Manager is not active employee", "Case Managed ECode"});

        foreach (var archivedSurvey in _archivedSurveys)
        {
            csvWriter.WriteField(archivedSurvey.Item1.CaseCode);
            csvWriter.WriteField(archivedSurvey.Item1.CaseName);
            csvWriter.WriteField(archivedSurvey.Item1.Modified);
            csvWriter.WriteField(archivedSurvey.Item1.EndDate);
            csvWriter.WriteField(archivedSurvey.Item2 ? "Yes" : "No");
            csvWriter.WriteField(archivedSurvey.Item3 ? "Yes" : "No");
            csvWriter.WriteField(archivedSurvey.Item1.ManagerEcode);
            csvWriter.NextRecord();
        }
    }

    public void WriteDifferentEcodesReport(TextWriter writer)
    {
        using var csvWriter = new CsvWriter(writer, CultureInfo.CurrentCulture);
        WriteHeader(csvWriter, new[] { "Case Code", "Case Name", "CCM Billing Partner", "Leap Billing Partner",
            "CCM Client Head", "Leap Client Head", "CCM Case Manager", "Leap Case Manager" });

        foreach (var (key, value) in _differentECodes)
        {
            csvWriter.WriteField(key.CaseCode);
            csvWriter.WriteField(key.CaseName);
            var billing = value.Find(e => e.PropertyName == nameof(LeapMasterRecord.BillingPartnerEcode));
            csvWriter.WriteField(billing?.CcmValue ?? "OK");
            csvWriter.WriteField(billing?.LeapValue ?? "OK");

            var clientHead = value.Find(e => e.PropertyName == nameof(LeapMasterRecord.ClientHeadEcode));
            csvWriter.WriteField(clientHead?.CcmValue ?? "OK");
            csvWriter.WriteField(clientHead?.LeapValue ?? "OK");

            var manager = value.Find(e => e.PropertyName == nameof(LeapMasterRecord.ManagerEcode));
            csvWriter.WriteField(manager?.CcmValue ?? "OK");
            csvWriter.WriteField(manager?.LeapValue ?? "OK");

            csvWriter.NextRecord();
        }
    }

    public void WriteDuplicates(TextWriter writer)
    {
        using var csvWriter = new CsvWriter(writer, CultureInfo.CurrentCulture);
        WriteHeader(csvWriter, new[] { "Case Code", "Case Name", "Count" });

        foreach (var (key, value) in _duplicates)
        {
            csvWriter.WriteField(key.CaseCode);
            csvWriter.WriteField(key.CaseName);
            csvWriter.WriteField(value);
            csvWriter.NextRecord();
        }
    }

    public void WriteUnmappedStats(TextWriter writer)
    {
        using var csvWriter = new CsvWriter(writer, CultureInfo.CurrentCulture);

        WriteHeader(csvWriter, new[] { "Term Text", "Count" });

        foreach (var (key, value) in _unmappedStats.OrderByDescending(s => s.Value))
        {
            csvWriter.WriteField(key);
            csvWriter.WriteField(value);
            csvWriter.NextRecord();
        }
    }

    public void WriteUnmappedTagIds(TextWriter writer)
    {
        using var csvWriter = new CsvWriter(writer, CultureInfo.CurrentCulture);

        WriteHeader(csvWriter, new[] { "Tag Id", "Count" });

        foreach (var (key, value) in _unmappedTagIds.OrderByDescending(s => s.Value))
        {
            csvWriter.WriteField(key);
            csvWriter.WriteField(value);
            csvWriter.NextRecord();
        }
    }

    public void WriteErrors(TextWriter writer)
    {
        using var csvWriter = new CsvWriter(writer, CultureInfo.CurrentCulture);

        WriteHeader(csvWriter, Header);

        foreach (var (r, errors) in _errorsByRecord.OrderBy(r => r.Key.LineNumber))
        {
            csvWriter.WriteField(r.LineNumber.ToString("00000"));
            csvWriter.WriteField(r.UniqueID);
            csvWriter.WriteField(r.CaseCode);
            csvWriter.WriteField(r.CaseName);
            foreach (var error in errors)
            {
                csvWriter.WriteField(error.code);
                csvWriter.WriteField(error.message);
            }

            csvWriter.NextRecord();
        }
    }

    public void WriteSurveyReopens(TextWriter writer)
    {
        using var csvWriter = new CsvWriter(writer, CultureInfo.CurrentCulture);

        WriteHeader(csvWriter, new[] { "Case Code", "Case Name", "Case End Date" });

        foreach (var record in _surveyReopenings)
        {
            csvWriter.WriteField(record.CaseCode);
            csvWriter.WriteField(record.CaseName);
            csvWriter.WriteField(record.EndDate.GetValueOrDefault().ToString("MM/dd/yyyy"));
            csvWriter.NextRecord();
        }
    }

    public void WritePrimaryTaxonomyDiscrepancies(TextWriter writer)
    {
        using var csvWriter = new CsvWriter(writer, CultureInfo.CurrentCulture);

        WriteHeader(csvWriter,
            new[] { "Case Code", "Case Name", "Client Type in LEAP", "Client Type in CCM", "Case Type in LEAP", "Case Type in CCM" });

        foreach (var record in _differentTaxonomies)
        {
            csvWriter.WriteField(record.Key.CaseCode);
            csvWriter.WriteField(record.Key.CaseName);
            csvWriter.WriteField(record.Value.LeapClient);
            csvWriter.WriteField(record.Value.CcmClient);
            csvWriter.WriteField(record.Value.LeapCase);
            csvWriter.WriteField(record.Value.CcmCase);
            csvWriter.NextRecord();
        }
    }

    public void WriteTerminatedBillingPartners(TextWriter writer)
    {
        using var csvWriter = new CsvWriter(writer, CultureInfo.CurrentCulture);

        WriteHeader(csvWriter,
            new[] { "Case Code", "Case Name", "Billing Partner Ecode" });

        foreach (var record in _billingPartnerTerminated)
        {
            csvWriter.WriteField(record.LeapRecord.CaseCode);
            csvWriter.WriteField(record.LeapRecord.CaseName);
            csvWriter.WriteField(record.Entity.BillingPartnerEcode);
            csvWriter.NextRecord();
        }
    }

    private static void WriteHeader(CsvWriter csvWriter, string[] hdrs)
    {
        foreach (var hdr in hdrs)
            csvWriter.WriteField(hdr);

        csvWriter.NextRecord();
    }
}

internal record TaxonomyDiscrepancy
{
    public string? LeapClient { get; set; }
    public string? CcmClient { get; set; }
    public string? LeapCase { get; set; }
    public string? CcmCase { get; set; }
}

internal record ECodeDiscrepancy(
    string? PropertyName,
    string? CcmValue,
    string? LeapValue
);

internal class LeapMasterComparer : IEqualityComparer<LeapMasterRecord>
{
    public static readonly LeapMasterComparer Instance = new LeapMasterComparer();

    public bool Equals(LeapMasterRecord? x, LeapMasterRecord? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) return false;
        if (x.GetType() != y.GetType()) return false;
        return x.CaseCode == y.CaseCode && x.CaseName == y.CaseName;
    }

    public int GetHashCode(LeapMasterRecord obj) => HashCode.Combine(obj.CaseCode, obj.CaseName);
}