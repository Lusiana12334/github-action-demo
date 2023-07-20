using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PEXC.Case.Tools.Migration;

public class MigrationReportPrinter
{
    public MigrationContext Context { get; }

    private readonly ILogger<MigrationReportPrinter> _logger;

    private readonly MigrationOptions _options;

    public MigrationReportPrinter(
        MigrationContext context, 
        ILogger<MigrationReportPrinter> logger,
        IOptions<MigrationOptions> options)
    {
        Context = context;
        _logger = logger;
        _options = options.Value;
    }

    public void Print()
    {
        WriteErrorsToFile();
        WriteMissingTermsTaxonomyToFile();
        WriteTagIdStatsToFile();
        WritePrimaryTaxonomyDiscrepancies();
        WriteDuplicates();
        WriteCcmEcodeDifferences();
        WriteArchivedSurveys();
        WriteTerminatedBillingPartners();

        if (_options.RestoreSurveyOpeningToNew)
            WriteSurveyReopens();
    }

    private void WriteTerminatedBillingPartners()
    {
        if (!Context.HasBillingPartnerTerminated)
            return;

        var fileName = $"terminated_billing_partners_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.csv";
        _logger.LogInformation("Save terminated billing partners to '{fileName}'", fileName);
        using var fs = File.OpenWrite(fileName);
        using var textWriter = new StreamWriter(fs);
        Context.WriteTerminatedBillingPartners(textWriter);
    }

    private void WriteArchivedSurveys()
    {
        var fileName = $"archived_surveys_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.csv";
        _logger.LogInformation("Save archived surveys to '{fileName}'", fileName);
        using var fs = File.OpenWrite(fileName);
        using var textWriter = new StreamWriter(fs);
        Context.WriteArchivedSurveys(textWriter);
    }

    private void WritePrimaryTaxonomyDiscrepancies()
    {
        var fileName = $"discrepancies_in_primary_taxonomy_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.csv";
        _logger.LogInformation("Save discrepancies in primary taxonomy to '{fileName}'", fileName);
        using var fs = File.OpenWrite(fileName);
        using var textWriter = new StreamWriter(fs);
        Context.WritePrimaryTaxonomyDiscrepancies(textWriter);
    }

    private void WriteCcmEcodeDifferences()
    {
        if (!Context.HasDuplicates)
            return;

        var fileName = $"ccm_differences_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.csv";
        _logger.LogInformation("Save CCM Ecode discrepancies to '{fileName}'", fileName);
        using var fs = File.OpenWrite(fileName);
        using var textWriter = new StreamWriter(fs);
        Context.WriteDifferentEcodesReport(textWriter);
    }


    private void WriteDuplicates()
    {
        if (!Context.HasDuplicates)
            return;

        var fileName = $"duplicates_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.csv";
        _logger.LogInformation("Save duplicates to '{fileName}'", fileName);
        using var fs = File.OpenWrite(fileName);
        using var textWriter = new StreamWriter(fs);
        Context.WriteDuplicates(textWriter);
    }
    
    private void WriteMissingTermsTaxonomyToFile()
    {
        if (!Context.HasUnmappedStats)
            return;

        var fileName = $"missing_terms_stats_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.csv";
        _logger.LogInformation("Save missing taxonomy terms to '{fileName}'", fileName);
        using var fs = File.OpenWrite(fileName);
        using var textWriter = new StreamWriter(fs);
        Context.WriteUnmappedStats(textWriter);
    }

    private void WriteErrorsToFile()
    {
        var fileName = $"migration_errors_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.csv";
        _logger.LogInformation("Save invalid records to '{fileName}'", fileName);
        using var fs = File.OpenWrite(fileName);
        using var textWriter = new StreamWriter(fs);
        Context.WriteErrors(textWriter);
    }

    private void WriteSurveyReopens()
    {
        var fileName = $"survey_reopens_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.csv";
        _logger.LogInformation("Save list of records with survey reopen to '{fileName}'", fileName);
        using var fs = File.OpenWrite(fileName);
        using var textWriter = new StreamWriter(fs);
        Context.WriteSurveyReopens(textWriter);
    }

    private void WriteTagIdStatsToFile()
    {
        if (!Context.HasUnmappedTagIds)
            return;

        var fileName = $"tagIds_stats_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.csv";
        _logger.LogInformation("Save unmapped tag ids to '{fileName}'", fileName);
        using var fs = File.OpenWrite(fileName);
        using var textWriter = new StreamWriter(fs);
        Context.WriteUnmappedTagIds(textWriter);
    }
}