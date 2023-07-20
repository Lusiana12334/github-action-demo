using PEXC.Case.Domain;

namespace PEXC.Case.DataAccess.CosmosDB;

public interface IDataImportStateRepository<T> where T : IEntity
{
    Task<T?> GetState();
    Task UpdateState(T importState);
}