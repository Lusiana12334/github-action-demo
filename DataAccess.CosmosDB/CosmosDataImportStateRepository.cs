using PEXC.Case.Domain;

namespace PEXC.Case.DataAccess.CosmosDB;

internal class CosmosDataImportStateRepository<T> : IDataImportStateRepository<T> where T : IEntity
{
    private readonly ICosmosDbRepository _cosmosDbRepository;

    public CosmosDataImportStateRepository(ICosmosDbRepository cosmosDbRepository)
    {
        _cosmosDbRepository = cosmosDbRepository;
    }

    public Task<T?> GetState()
    {
        return _cosmosDbRepository
            .GetDocument<T>(typeof(T).Name, typeof(T).Name);
    }

    public Task UpdateState(T importState)
    {
        return _cosmosDbRepository.UpsertDocument(importState);
    }
}