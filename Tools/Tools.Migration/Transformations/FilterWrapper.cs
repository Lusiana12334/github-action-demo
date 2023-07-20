using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace PEXC.Case.Tools.Migration.Transformations;

public class FilterWrapper
{
    public static readonly TimeSpan QueueTakeMaxTime = TimeSpan.FromMinutes(10);

    public MigrationContext Context { get; }

    private readonly Func<MigrationData[], Task<IEnumerable<MigrationData>>> _processor;

    private readonly BlockingCollection<MigrationData> _inputCollection;

    private BlockingCollection<MigrationData>? _outputCollection;

    private readonly int _recordCount;

    private readonly ILogger<FilterWrapper> _logger;

    private int _processedRecords;

    private readonly Stopwatch _stopwatch = new Stopwatch();

    public string Name { get; }

    public string Performance
    {
        get
        {
            var milliseconds = _stopwatch.ElapsedMilliseconds == 0 ? 1 : _stopwatch.ElapsedMilliseconds;
            var speed = _processedRecords * 1000.0m / milliseconds;
            return $"{Name}: {_processedRecords} records in {_stopwatch.Elapsed}, speed: {speed:#.##} [rec/s]";
        }
    }

    public FilterWrapper(
        Func<MigrationData, Task<IEnumerable<MigrationData>>> processor,
        BlockingCollection<MigrationData> inputCollection,
        BlockingCollection<MigrationData>? outputCollection,
        MigrationContext context,
        string name,
        ILogger<FilterWrapper> logger)
        : this(arr =>
        {
            Debug.Assert(arr.Length == 1);
            return processor(arr[0]);
        }, inputCollection, outputCollection, context, name, 1, logger)
    { }

    public FilterWrapper(
        Func<MigrationData[], Task<IEnumerable<MigrationData>>> processor,
        BlockingCollection<MigrationData> inputCollection,
        BlockingCollection<MigrationData>? outputCollection,
        MigrationContext context,
        string name,
        int recordCount,
        ILogger<FilterWrapper> logger)
    {
        Context = context;
        _processor = processor;
        _inputCollection = inputCollection;
        _outputCollection = outputCollection;
        _recordCount = recordCount;
        _logger = logger;
        Name = name;
    }

    public FilterWrapper Join(
        Func<MigrationData, Task<IEnumerable<MigrationData>>> processor,
        MigrationContext context,
        string name,
        ILogger<FilterWrapper> logger)
    {
        if (_outputCollection == null)
            _outputCollection = new BlockingCollection<MigrationData>(500);
        return new FilterWrapper(processor, _outputCollection, null, context, name, logger);
    }

    public async Task Run()
    {
        try
        {
            var buffer = new List<MigrationData>();
            while (!_inputCollection.IsCompleted)
            {
                if (!_inputCollection.TryTake(out var toProcess, QueueTakeMaxTime))
                    continue;

                buffer.Add(toProcess);

                if (buffer.Count < _recordCount)
                    continue;

                await ProcessBuffer(buffer);
            }

            if (buffer.Count > 0)
                await ProcessBuffer(buffer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during processing pipeline {name}", Name);
        }
        finally
        {
            _outputCollection?.CompleteAdding();
        }

        _logger.LogInformation(Performance);
    }

    private async Task ProcessBuffer(List<MigrationData> buffer)
    {
        try
        {
            _stopwatch.Start();
            var result = await _processor(buffer.ToArray());
            _stopwatch.Stop();

            _processedRecords += buffer.Count;

            foreach (var item in result)
                _outputCollection?.TryAdd(item, QueueTakeMaxTime);
        }
        catch (Exception ex)
        {
            foreach (var migrationData in buffer)
                Context.AddException(migrationData.LeapRecord, ex);
        }
        finally
        {
            buffer.Clear();
        }
    }
}