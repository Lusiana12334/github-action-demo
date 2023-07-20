namespace PEXC.Case.Services.IRIS.Contracts;

public record ApiResult<T>
{
    public int TotalCount { get; init; }
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
}