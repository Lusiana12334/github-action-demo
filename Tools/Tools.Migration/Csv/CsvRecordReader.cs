using System.Globalization;
using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using PEXC.Case.Domain;

namespace PEXC.Case.Tools.Migration.Csv;

public class CsvRecordReader : IDisposable
{
    private readonly IMapper _mapper;

    private CsvReader? _csvReader;

    public CsvRecordReader(IMapper mapper)
    {
        _mapper = mapper;
    }

    public async IAsyncEnumerable<MigrationData> ReadRecords(TextReader streamReader)
    {
        var records = Read(streamReader);

        await foreach (var record in records)
        {
            var entity = _mapper.Map<CaseEntity>(record);
            var migrationData = new MigrationData(record, entity);

            yield return migrationData;
        }
    }

    public IAsyncEnumerable<LeapMasterRecord> Read(TextReader reader)
    {
        _csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);
        _csvReader.Context.Configuration.TrimOptions = TrimOptions.Trim;
        var records = _csvReader.GetRecordsAsync<LeapMasterRecord>();


        _csvReader.Context.TypeConverterOptionsCache.GetOptions<string>().NullValues.Add(string.Empty);
        AddTrueFalseValues(_csvReader.Context.TypeConverterOptionsCache.GetOptions<bool>());
        AddTrueFalseValues(_csvReader.Context.TypeConverterOptionsCache.GetOptions<bool?>());

        return records;
    }

    private static void AddTrueFalseValues(TypeConverterOptions options)
    {
        options.BooleanTrueValues.AddRange(new[] { "Yes", "True" });
        options.BooleanFalseValues.AddRange(new[] { "No", "False", "0" });
    }

    public void Dispose() => _csvReader?.Dispose();
}