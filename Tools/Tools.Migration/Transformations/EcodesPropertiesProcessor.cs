using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PEXC.Case.Services.CCM.Contracts;

namespace PEXC.Case.Tools.Migration.Transformations;

public class EcodesPropertiesProcessor
{
    private static readonly string[] EcodeProperties =
    {
        nameof(LeapMasterRecord.ClientHeadEcode),
        nameof(LeapMasterRecord.BillingPartnerEcode),
        nameof(LeapMasterRecord.KMContactEcode),
        nameof(LeapMasterRecord.ManagerEcode),
    };

    private readonly ECodeLoader _eCodeLoader;
    private readonly MigrationContext _context;

    private readonly ILogger<EcodesPropertiesProcessor> _logger;

    private Dictionary<string, (string Name, bool IsTerminated)>? _eCodes;

    public EcodesPropertiesProcessor(
        ECodeLoader eCodeLoader,
        MigrationContext context,
        ILogger<EcodesPropertiesProcessor> logger)
    {
        _eCodeLoader = eCodeLoader;
        _context = context;
        _logger = logger;
    }

    public async Task Init()
    {
        _logger.LogInformation("Loading existing ECodes from ProfileDb");
        var sw = Stopwatch.StartNew();
        _eCodes = await _eCodeLoader.Load();
        sw.Stop();
        _logger.LogInformation("Loaded {count} items in {time}", _eCodes.Count, sw.Elapsed);
    }

    public Task<IEnumerable<MigrationData>> CheckAndUpdateEcodes(MigrationData record)
    {
        var leapRecord = record.LeapRecord;

        if (record.CcmData != null)
            UpdateUsingCcmValues(leapRecord, record.CcmData);

        CheckEcodesAreValid(leapRecord, EcodeProperties);
        var managerInfo = _eCodes!.GetValueOrDefault(leapRecord.ManagerEcode);
        record.IsCaseManagerTerminated = managerInfo.IsTerminated;

        var billingPartnerInfo = _eCodes!.GetValueOrDefault(leapRecord.BillingPartnerEcode ?? "");

        if (billingPartnerInfo.IsTerminated || string.IsNullOrEmpty(leapRecord.BillingPartnerEcode))
            _context.AddBillingPartnerTerminated(record);

        return Task.FromResult(EnumerableEx.Return(record));
    }

    private void UpdateUsingCcmValues(LeapMasterRecord record, CaseDetailsDto ccmData)
    {
        record.ClientHeadEcode = ccmData.GlobalCoordinatingPartner;
        record.ManagerEcode = ccmData.CaseManager;
        record.BillingPartnerEcode = ccmData.BillingPartner;
    }

    private void CheckEcodesAreValid(LeapMasterRecord record, params string[] properties)
    {
        foreach (var property in properties)
        {
            var ecode = record.GetProperty(property);
            if (string.IsNullOrWhiteSpace(ecode))
                _context.AddMissingEcode(record, property);
            else if (ecode.Contains(';'))
                _context.AddMultipleEcodes(record, property, ecode);
            else if (!IsValidEcode(ecode.Trim()))
                _context.AddInvalidEcode(record, property, ecode.Trim());
        }

        foreach (var expertEcode in record.BainExpertsEcodes)
            if (!IsValidEcode(expertEcode))
                _context.AddInvalidEcode(record, nameof(LeapMasterRecord.BainExpertsEcodes), expertEcode);

        foreach (var partnerEcode in record.OperatingPartnerEcodes)
            if (!IsValidEcode(partnerEcode))
                _context.AddInvalidEcode(record, nameof(LeapMasterRecord.OperatingPartnerEcodes), partnerEcode);
    }

    private bool IsValidEcode(string ecode)
        => _eCodes!.ContainsKey(ecode);
}