namespace PEXC.Case.Domain;

public record IrisDataImportState : BaseDataImportState, IEntity
{
    public string Id => nameof(IrisDataImportState);
    public string Key => nameof(IrisDataImportState);
    public string Type => nameof(IrisDataImportState);

    public Guid CorrelationId { get; set; }
}