namespace PEXC.Case.Domain;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    
    public string? NextPageToken { get; set; }
}