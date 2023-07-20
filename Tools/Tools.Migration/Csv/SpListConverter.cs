using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace PEXC.Case.Tools.Migration.Csv;

public class SpListConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<string>();
        return text.Split(";#", StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
    }
}

public class ListConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<string>();
        return text.Split(";", StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
    }
}


public class TrimConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;
        return text.Trim();
    }
}