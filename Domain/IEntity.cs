namespace PEXC.Case.Domain;

public interface IEntity
{
    string Id { get; }

    string Key { get;  }
    
    string Type { get; }

    Guid CorrelationId { get; set; }
}