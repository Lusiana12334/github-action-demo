namespace PEXC.Case.Domain;

public class AuditEntry<TEntity>: IEntity where TEntity : IEntity
{
    public string Id { get; set; }
    public string Key => AuditEntity.Id;
    public string Type => AuditEntity.Type;
    public Guid CorrelationId { get; set; }
    public TEntity AuditEntity { get; set; }
    public DateTime Created { get; set; }

    public AuditEntry(TEntity auditEntity)
    {
        Id = Guid.NewGuid().ToString();
        AuditEntity = auditEntity;
        Created = DateTime.UtcNow;
    }
}