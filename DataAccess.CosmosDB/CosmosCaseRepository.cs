using System.Linq.Expressions;
using PEXC.Case.Domain;

namespace PEXC.Case.DataAccess.CosmosDB;

internal class CosmosCaseRepository : ICaseRepository
{
    private readonly ICosmosDbRepository _cosmosDbRepository;

    public CosmosCaseRepository(ICosmosDbRepository cosmosDbRepository)
    {
        _cosmosDbRepository = cosmosDbRepository;
    }

    public Task<PagedResult<CaseEntity>> GetCasesReadyForSearch(
        TimeSpan gracePeriod,
        int? pageSize = null,
        string? nextPageToken = null)
    {
        var endDateThreshold = DateTime.Today - gracePeriod;
        Expression<Func<CaseEntity, bool>> caseSearchablePredicate =
            caseEntity =>
                caseEntity.Type == nameof(CaseEntity) &&
                caseEntity.ItemStage == CaseState.Published &&
                (!caseEntity.IsSearchable.HasValue || !caseEntity.IsSearchable.Value) &&
                (caseEntity.EndDate.HasValue && caseEntity.EndDate < endDateThreshold);

        return _cosmosDbRepository.Query(predicate: caseSearchablePredicate, pageSize, nextPageToken, includeDeletedItems: true);
    }

    public Task<PagedResult<CaseEntity>> GetCasesRemovedFromSearch(
        int? pageSize = null,
        string? nextPageToken = null)
    {
        Expression<Func<CaseEntity, bool>> caseRemovedFromSearchPredicate =
            caseEntity =>
                caseEntity.Type == nameof(CaseEntity) &&
                (caseEntity.IsSearchable.HasValue && caseEntity.IsSearchable.Value) &&
                caseEntity.ItemStage != CaseState.Published;

        return _cosmosDbRepository.Query(predicate: caseRemovedFromSearchPredicate, pageSize, nextPageToken, includeDeletedItems: true);
    }

    public Task<PagedResult<CaseEntity>> GetCasesAfterConfidentialGracePeriod(
        TimeSpan gracePeriod,
        int? pageSize = null,
        string? nextPageToken = null)
    {
        var endDateThreshold = DateTime.Today - gracePeriod;
        Expression<Func<CaseEntity, bool>> caseAfterGracePeriod =
            caseEntity =>
                caseEntity.Type == nameof(CaseEntity) &&
                (caseEntity.IsSearchable.HasValue && caseEntity.IsSearchable.Value) &&
                (!caseEntity.IsInConfidentialGracePeriod.HasValue || caseEntity.IsInConfidentialGracePeriod.Value) &&
                (caseEntity.EndDate.HasValue && caseEntity.EndDate < endDateThreshold);

        return _cosmosDbRepository.Query(predicate: caseAfterGracePeriod, pageSize, nextPageToken);
    }

    public Task<PagedResult<CaseEntity>> GetSearchableCases(
        DateTime? modifiedSince = null,
        int? pageSize = null,
        string? nextPageToken = null)
    {
        Expression<Func<CaseEntity, bool>> searchableCasePredicate =
            caseEntity =>
                caseEntity.Type == nameof(CaseEntity) &&
                (modifiedSince.HasValue
                    ? caseEntity.Modified > modifiedSince
                    : caseEntity.ItemStage != CaseState.Deleted &&
                      caseEntity.IsSearchable.HasValue && caseEntity.IsSearchable.Value);

        return _cosmosDbRepository.Query(predicate: searchableCasePredicate, pageSize, nextPageToken, includeDeletedItems: true);
    }

    public Task<PagedResult<CaseEntity>> GetActiveCases(
        DateTime? modifiedSince = null,
        int? pageSize = null,
        string? nextPageToken = null)
    {
        Expression<Func<CaseEntity, bool>> activeCasePredicate =
            caseEntity =>
                caseEntity.Type == nameof(CaseEntity) &&
                (modifiedSince.HasValue
                    ? caseEntity.Modified > modifiedSince
                    : caseEntity.ItemStage != CaseState.Deleted);

        return _cosmosDbRepository.Query(predicate: activeCasePredicate, pageSize, nextPageToken, includeDeletedItems: true);
    }
    
    public async Task<List<CaseEntity>> GetCasesById(List<(string Id, string Key)> identifiers)
    {
        var caseParam = Expression.Parameter(typeof(CaseEntity), "c");

        var idProp = Expression.Property(caseParam, nameof(CaseEntity.Id));
        var keyProp = Expression.Property(caseParam, nameof(CaseEntity.Key));
        var typeProperty = Expression.Property(caseParam, nameof(CaseEntity.Type));

        Expression? start = null;

        foreach (var identifier in identifiers)
        {
            var condition = Expression.AndAlso(Expression.Equal(idProp, Expression.Constant(identifier.Id)),
                Expression.Equal(keyProp, Expression.Constant(identifier.Key)));

            start = start == null ? condition : Expression.OrElse(condition, start);
        }

        var typeCondition = Expression.Equal(typeProperty, Expression.Constant(nameof(CaseEntity)));
        var predicateBody = Expression.AndAlso(typeCondition, start!);
        var predicate = Expression.Lambda<Func<CaseEntity, bool>>(predicateBody, caseParam);
        return (await _cosmosDbRepository.Query(predicate, identifiers.Count)).Items.ToList();
    }
}