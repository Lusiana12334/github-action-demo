namespace PEXC.Case.Tools.Migration;

static class EnumerableEx
{
    public static IEnumerable<T> Return<T>(T item)
    {
        yield return item;
    }
}