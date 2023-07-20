using System.Linq.Expressions;
using PEXC.Case.Domain;

namespace PEXC.Case.DataAccess.CosmosDB;

public static class CosmosDbRepositoryExtensions
{
    public static Task<PagedResult<T>> Query<T>(
        this ICosmosDbRepository @this,
        Expression<Func<T, bool>>? predicate = null,
        int? pageSize = null,
        string? nextPageToken = null,
        string? partitionKey = null,
        bool includeDeletedItems = false) where T: IEntity =>
        @this.Query(x => x, predicate, pageSize, nextPageToken, partitionKey, includeDeletedItems);
}