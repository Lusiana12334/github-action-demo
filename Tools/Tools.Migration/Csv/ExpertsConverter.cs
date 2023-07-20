using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace PEXC.Case.Tools.Migration.Csv;

public class ExpertsConverter : DefaultTypeConverter
{
    private static readonly Regex ECodeExtractor = new Regex(@"bain\\(?<ecode>\w+?);#", RegexOptions.Compiled);

    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        return string.IsNullOrEmpty(text)
            ? Array.Empty<string>()
            : ECodeExtractor.Matches(text).Select(m => m.Groups["ecode"].Value.Trim()).ToList();
    }
}