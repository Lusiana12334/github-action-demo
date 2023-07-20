using System.Linq.Expressions;
using PEXC.Case.Domain;

namespace PEXC.Case.DataAccess.CosmosDB;

public interface ICosmosDbRepository
{
    public Task<T> CreateDocument<T>(T document) where T : IEntity;
    public Task<T> UpsertDocument<T>(T document) where T : IEntity;
    public Task<T?> GetDocument<T>(string id, string partitionKey) where T : IEntity;
    public Task<T> PatchDocument<T>(string id, string partitionKey, IReadOnlyDictionary<string, object?> propertiesToUpdate) where T : IEntity;
    public Task<T> HardDeleteDocument<T>(string id, string partitionKey, Guid correlationId) where T: IEntity;
    public Task<PagedResult<TResult>> Query<T, TResult>(
        Expression<Func<T, TResult>> selector,
        Expression<Func<T, bool>>? predicate = null,
        int? pageSize = null,
        string? nextPageToken = null,
        string? partitionKey = null,
        bool includeDeletedItems = false
        ) where T : IEntity;
    Task<bool> CanConnect();
}