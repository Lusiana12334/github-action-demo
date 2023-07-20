namespace PEXC.Case.Domain;

public abstract record BaseDataImportState
{
    public DateTime? LastExecutionTime { get; set; }
    public DateOnly? LastModifiedAfter { get; set; }
    public DateTime? LastSuccessfulExecutionTime { get; set; }
    public DateOnly? LastSuccessfulModifiedAfter { get; set; }
    public ICollection<string> UpdatedCases { get; } = new List<string>();
    public ICollection<string> CreatedCases { get; } = new List<string>();
    public bool Failed { get; set; }
    public string? OperationId { get; set; }
    public int FailedAttempts { get; set; }
    public string? ErrorMessage { get; set; }
}