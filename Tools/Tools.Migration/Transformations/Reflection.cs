using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace PEXC.Case.Tools.Migration.Transformations;

public static class Reflection
{
    private static readonly ConcurrentDictionary<string, Func<LeapMasterRecord, string>> Accessors = new();

    public static string GetProperty(this LeapMasterRecord record, string propertyName)
    {
        var accessor = Accessors.GetOrAdd(propertyName, propName =>
        {
            var param = Expression.Parameter(typeof(LeapMasterRecord));
            var propAccessor = Expression.Property(param, propName);
            return Expression.Lambda<Func<LeapMasterRecord, string>>(propAccessor, param).Compile();
        });

        return accessor(record);
    }
}