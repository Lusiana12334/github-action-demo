using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PEXC.Case.Domain;
using PEXC.Case.Services.CCM.Contracts;
using PEXC.Case.Tools.Migration.Csv;
using PEXC.Case.Tools.Migration.Transformations;
using PEXC.Document.Client;
using PEXC.Document.DataContracts.V1;
using static PEXC.Case.Services.Workflow.CaseDocumentHelper;

namespace PEXC.Case.Tools.Migration;

public class RecordProcessor
{
    public const int BatchSize = 10;

    private readonly CsvRecordReader _reader;

    private readonly CcmLoader _ccmLoader;

    private readonly TaxonomyDataMapper _taxonomyMapper;

    private readonly IDocumentServiceClient _documentService;

    private readonly EcodesPropertiesProcessor _ecodesPropertiesProcessor;

    private readonly ILogger<RecordProcessor> _logger;

    private readonly ILoggerFactory _loggerFactory;

    private readonly IRandomizer _randomizer;

    private readonly BlockingCollection<MigrationData> _processingQueue = new BlockingCollection<MigrationData>(500);

    private readonly BlockingCollection<MigrationData> _taxonomyQueue = new BlockingCollection<MigrationData>(500);

    private readonly BlockingCollection<MigrationData> _randomizerQueue = new BlockingCollection<MigrationData>(500);

    private readonly BlockingCollection<MigrationData> _documentQueue = new BlockingCollection<MigrationData>(500);

    private readonly BlockingCollection<MigrationData> _saveDbQueue = new BlockingCollection<MigrationData>(500);

    private readonly BlockingCollection<MigrationData> _reopenSurvey = new BlockingCollection<MigrationData>(500);

    private readonly BlockingCollection<MigrationData> _uniqueRecords = new BlockingCollection<MigrationData>(500);

    private readonly BlockingCollection<MigrationData> _ecodeValidation = new BlockingCollection<MigrationData>(500);

    private readonly MigrationDataPersister _repository;

    public MigrationContext Context { get; }

    private readonly MigrationOptions _options;

    public RecordProcessor(
        CsvRecordReader reader,
        CcmLoader ccmLoader,
        MigrationDataPersister repository,
        TaxonomyDataMapper taxonomyMapper,
        IDocumentServiceClient documentService,
        EcodesPropertiesProcessor ecodesPropertiesProcessor,
        MigrationContext context,
        ILoggerFactory loggerFactory,
        IRandomizer randomizer,
        IOptions<MigrationOptions> options)
    {
        _reader = reader;
        _ccmLoader = ccmLoader;
        _repository = repository;
        _taxonomyMapper = taxonomyMapper;
        _documentService = documentService;
        _ecodesPropertiesProcessor = ecodesPropertiesProcessor;
        _loggerFactory = loggerFactory;
        _randomizer = randomizer;
        _logger = loggerFactory.CreateLogger<RecordProcessor>();
        Context = context;
        _options = options.Value;
    }

    public async Task Process(TextReader streamReader)
    {
        await LoadInitialData();

        var filterLogger = _loggerFactory.CreateLogger<FilterWrapper>();
        var lastQueue = _options.CreateDirectories ? _documentQueue : _saveDbQueue;
        var filters = new List<FilterWrapper>
        {
            new FilterWrapper(_ccmLoader.LoadCcmApi, _processingQueue, _uniqueRecords, Context, "Load CCM", BatchSize, filterLogger),
            new FilterWrapper(HandleDuplicates, _uniqueRecords, _taxonomyQueue, Context, "Handle duplicates", filterLogger),
            new FilterWrapper(MapTaxonomy, _taxonomyQueue, _ecodeValidation, Context, "Map Taxonomy", filterLogger),
            new FilterWrapper(_ecodesPropertiesProcessor.CheckAndUpdateEcodes, _ecodeValidation, _reopenSurvey, Context, "Ecode Validator", filterLogger),
            new FilterWrapper(ApplyAdditionalTransforms, _reopenSurvey, _randomizerQueue, Context, "Reopen Survey", filterLogger),
            new FilterWrapper(_randomizer.RandomizeData, _randomizerQueue, lastQueue, Context, "Randomizer", filterLogger),
        };

        if (_options.CreateDirectories)
        {
            filters.Add(
                new FilterWrapper(CreateCaseDirectory, _documentQueue, _saveDbQueue, Context, "Create directory", 8, filterLogger)
            );
        }

        filters.Add(new FilterWrapper(_repository.PersistRecords, _saveDbQueue, null, Context, "Persist records", 8, filterLogger));

        var tasks =
            new[] { Task.Run(() => FilterWrapper(() => _reader.ReadRecords(streamReader), _processingQueue)) }
            .Concat(filters.Select(f => Task.Run(f.Run)))
            .ToArray();

        _logger.LogInformation("Starting Migration...");
        var printer = new ProgressReportPrinter();

        while (!Task.WaitAll(tasks, TimeSpan.FromSeconds(1)))
            printer.PrintReport(_repository.ProcessedRecords, filters);

        printer.PrintReport(_repository.ProcessedRecords, filters);

        _logger.LogInformation("Migration Finished, new record: {new_records}, updated_records: {updated_records}...",
            _repository.NewRecords, _repository.UpdatedRecords);
    }

    private async Task LoadInitialData()
    {
        _logger.LogInformation("Initializing pipeline steps...");
        var start = DateTime.UtcNow;
        await Task.WhenAll(
            _taxonomyMapper.Init(),
            _repository.Init(),
            _ecodesPropertiesProcessor.Init()
        );
        var time = DateTime.UtcNow - start;
        _logger.LogInformation("Pipeline steps initialized in {time}", time);
    }

    public async Task FilterWrapper<T>(Func<IAsyncEnumerable<T>> processor, BlockingCollection<T> collection)
    {
        try
        {
            await foreach (var record in processor())
                if (!collection.TryAdd(record, Transformations.FilterWrapper.QueueTakeMaxTime))
                {
                    throw new TimeoutException("Cannot add item to queue");
                }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during processing");
        }
        finally
        {
            collection.CompleteAdding();
        }
    }

    private readonly HashSet<string> _recordIds = new HashSet<string>();
    private readonly HashSet<string> _migrationIds = new HashSet<string>();

    private Task<IEnumerable<MigrationData>> HandleDuplicates(MigrationData record)
    {
        if (string.IsNullOrEmpty(record.LeapRecord.ID))
            throw new InvalidOperationException($"Leap record is empty: {record.LeapRecord}");

        if (!_migrationIds.Add(record.LeapRecord.ID!))
            throw new InvalidOperationException($"There cannot be more than one case with identical id: {record.LeapRecord.ID}");

        if (string.Equals(record.LeapRecord.DuplicateRecordForMigration, "skip", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(Enumerable.Empty<MigrationData>());

        if (!_recordIds.Add(record.LeapRecord.CaseCode + "|" + record.LeapRecord.CaseName + "|" + record.LeapRecord.RelationshipType))
        {
            Context.AddDuplicate(record.LeapRecord);
            return Task.FromResult(Enumerable.Empty<MigrationData>());
        }

        return Task.FromResult(EnumerableEx.Return(record));
    }

    private Task<IEnumerable<MigrationData>> ApplyAdditionalTransforms(MigrationData record)
    {
        if (record.LeapRecord.RelationshipType == RelationshipType.NonRetainer
            && record.CcmData != null)
        {
            record.Entity.ClientId = record.CcmData.ClientId.ToString();
        }

        return ApplyOpenSurveyPolicies(record);
    }

    private Task<IEnumerable<MigrationData>> ApplyOpenSurveyPolicies(MigrationData record)
    {
        if (record.Entity.ItemStage != CaseState.SurveyOpened)
            return Task.FromResult(EnumerableEx.Return(record));

        var recordIsOld = record.LeapRecord.EndDate < _options.OpenedSurveyArchiveDate;
        if (recordIsOld || record.IsCaseManagerTerminated)
        {
            record.Entity.ItemStage = CaseState.SurveyOpenedArchive;
            Context.AddSurveyArchive(record.LeapRecord, recordIsOld, record.IsCaseManagerTerminated);
        }
        else if (_options.RestoreSurveyOpeningToNew)
        {
            Context.AddSurveyReopen(record.LeapRecord);
            record.Entity.ItemStage = CaseState.New;
        }
        return Task.FromResult(EnumerableEx.Return(record));
    }

    private async Task<IEnumerable<MigrationData>> CreateCaseDirectory(MigrationData[] records)
    {
        var tasks = new List<Task<DirectoryInfoDto>>();

        foreach (var data in records)
        {
            var createDirectoryDto = new CreateDirectoryDto(CreateDirectoryName(data.Entity.CaseCode, data.Entity.CaseName!, data.Entity.UniqueId));
            var correlationId = data.Entity.CorrelationId.ToString();
            tasks.Add(_documentService.CreateDirectory(createDirectoryDto, correlationId));
        }

        var result = await Task.WhenAll(tasks);
        for (var i = 0; i < records.Length; i++)
            records[i].Entity.SharePointDirectory = new SharePointDirectoryEntity
            {
                Url = result[i].Url,
                DirectoryId = result[i].DirectoryId,
                DriveId = result[i].DriveId,
            };

        return records.AsEnumerable();
    }

    private Task<IEnumerable<MigrationData>> MapTaxonomy(MigrationData record)
    {
        try
        {
            _taxonomyMapper.ChangeTaxonomy(record.Entity, record.LeapRecord, record.CcmData);
        }
        catch (Exception ex)
        {
            return Task.FromException<IEnumerable<MigrationData>>(ex);
        }

        return Task.FromResult(EnumerableEx.Return(record));
    }
}

public record MigrationData(LeapMasterRecord LeapRecord, CaseEntity Entity)
{
    public CaseDetailsDto? CcmData { get; set; }
    public bool IsCaseManagerTerminated { get; set; }
}