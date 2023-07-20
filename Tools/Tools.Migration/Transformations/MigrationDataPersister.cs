using PEXC.Case.DataAccess.CosmosDB;

namespace PEXC.Case.Tools.Migration.Transformations;

public class MigrationDataPersister
{
    private readonly ICosmosDbRepository _dbRepository;

    private readonly ExistingDataLoader _dataLoader;

    public int ProcessedRecords { get; private set; }

    public int NewRecords { get; private set; }

    public int UpdatedRecords { get; private set; }

    public MigrationDataPersister(ICosmosDbRepository dbRepository, ExistingDataLoader dataLoader)
    {
        _dbRepository = dbRepository;
        _dataLoader = dataLoader;
    }

    public Task Init()
        => _dataLoader.Init();

    public async Task<IEnumerable<MigrationData>> PersistRecords(MigrationData[] records)
    {
        foreach (var record in records)
        {
            var entity = record.Entity;

            if (_dataLoader.TryGetRecord(record.LeapRecord.ID!, out var dbId))
            {
                entity.Id = dbId.Id!;
                UpdatedRecords++;
            }
            else
            {
                NewRecords++;
            }
            ProcessedRecords++;
        }

        await Task.WhenAll(records.Select(r => r.Entity).Select(entity =>
            _dbRepository.UpsertDocument(entity)
        ));

        return Enumerable.Empty<MigrationData>();
    }
}