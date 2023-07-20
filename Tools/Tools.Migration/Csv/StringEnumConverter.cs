using System.Reflection;
using System.Runtime.Serialization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace PEXC.Case.Tools.Migration.Csv;

public class StringEnumConverter<T> : DefaultTypeConverter where T : struct, Enum
{
    private static IDictionary<string, T>? _nameToEnum;

    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        _nameToEnum ??= InitNames();

        if (string.IsNullOrEmpty(text))
            throw new InvalidOperationException($"Invalid {typeof(T).Name} state - value is empty");

        if (_nameToEnum.TryGetValue(text.Trim(), out var result))
            return result;

        throw new InvalidOperationException($"Invalid {typeof(T).Name} state value is: {text}");
    }

    private IDictionary<string, T> InitNames() =>
        Enum.GetNames(typeof(T)).Select(GetField)
            .ToDictionary(GetName, f => (T)f.GetValue(null)!, StringComparer.OrdinalIgnoreCase);

    private static string GetName(FieldInfo f) =>
        f.GetCustomAttributes(typeof(EnumMemberAttribute), true)
            .Cast<EnumMemberAttribute>()
            .Select(a => a.Value!)
            .Single();

    private static FieldInfo GetField(string name) =>
        typeof(T).GetField(name,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)!;
}