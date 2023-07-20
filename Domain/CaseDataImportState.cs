namespace PEXC.Case.Domain;

public record CaseDataImportState : BaseDataImportState, IEntity
{
    public string Id => nameof(CaseDataImportState);
    public string Key => nameof(CaseDataImportState);
    public string Type => nameof(CaseDataImportState);
    
    public Guid CorrelationId { get; set; }
}