using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using PEXC.Case.DataAccess.CosmosDB;
using PEXC.Case.Domain;

namespace PEXC.Case.Tools.Migration.Transformations;

public class ExistingDataLoader
{
    private readonly ILogger<ExistingDataLoader> _logger;

    private readonly ICosmosDbRepository _dbRepository;

    private Dictionary<string, MigrationDbData>? _existingRecordIds;

    public ExistingDataLoader(ILogger<ExistingDataLoader> logger, ICosmosDbRepository dbRepository)
    {
        _logger = logger;
        _dbRepository = dbRepository;
    }

    public async Task Init()
    {
        _logger.LogInformation("Loading existing cases from CosmosDb");

        var sw = Stopwatch.StartNew();

        var result = await _dbRepository.Query<CaseEntity, MigrationDbData>(
            c => new MigrationDbData { Id = c.Id, MigrationId = c.HistoricFields!.MigrationId!, Key = c.Key, CaseName = c.CaseName },
            c => c.HistoricFields!.MigrationId != null && c.Type == nameof(CaseEntity));

        _existingRecordIds = result.Items.ToDictionary(d => d.MigrationId!);

        sw.Stop();

        _logger.LogInformation("Loaded {count} items in {time}", _existingRecordIds.Count, sw.Elapsed);
    }

    internal bool TryGetRecord(string recordId, [MaybeNullWhen(false)] out MigrationDbData dbData)
    {
        if (_existingRecordIds == null)
            throw new InvalidOperationException("Please call Init() first");

        return _existingRecordIds.TryGetValue(recordId, out dbData);
    }
}

internal class MigrationDbData
{
    public string? Id { get; set; }
    public string? MigrationId { get; set; }
    public string? Key { get; set; }
    public string? CaseName { get; set; }
}