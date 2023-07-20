using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PEXC.Case.DataAccess;
using PEXC.Case.Domain;
using PEXC.Case.Services.Infrastructure;
using PEXC.Common.Logging.Extensions;

namespace PEXC.Case.Services;

public class CaseSearchabilityService : ICaseSearchabilityService
{
    public const string SearchableCasesCrawlerServiceUserName = "Searchable Cases Crawler Service";
    private static readonly UserInfo SearchableCasesCrawlerUserInfo
        = new(UserType.Service, SearchableCasesCrawlerServiceUserName);

    private readonly ILogger<CaseSearchabilityService> _logger;

    private readonly CaseSearchabilityOptions _searchabilityOptions;

    private readonly ICaseRepository _caseRepository;

    private readonly ISingleCaseRepository _singleCaseRepository;

    public CaseSearchabilityService(
        IOptions<CaseSearchabilityOptions> searchabilityOptions, 
        ILogger<CaseSearchabilityService> logger, 
        ICaseRepository caseRepository, 
        ISingleCaseRepository singleCaseRepository)
    {
        _searchabilityOptions = searchabilityOptions.Value;
        _logger = logger;
        _caseRepository = caseRepository;
        _singleCaseRepository = singleCaseRepository;
    }

    public async Task UpdateCasesSearchability()
    {
        try
        {
            await RemoveCasesFromSearchIndex();
            await AddCasesToSearchIndex();
            await RemoveCasesFromConfidentialGracePeriod();
        }
        catch (Exception e)
        {
            _logger.LogErrorWithTelemetry(e, "Updating Cases Searchability failed.", e.Message);
            throw;
        }
    }

    private async Task RemoveCasesFromSearchIndex()
    {
        PagedResult<CaseEntity>? caseEntities = null;
        do
        {
            _logger.LogInformation("Querying for Cases that should be removed from Coveo search index.");
            caseEntities = await _caseRepository.GetCasesRemovedFromSearch(
                _searchabilityOptions.DatabaseQueryPageSize,
                caseEntities?.NextPageToken);
            _logger.LogInformation("Found {count} cases that should be removed " +
                                   "from Coveo search index. Has next page: {hasNextPage}",
                                    caseEntities.Items.Count, caseEntities.NextPageToken != null);
            foreach (var caseEntity in caseEntities.Items)
            {
                await UpdateCaseSearchability(caseEntity, false);
            }
        } while (caseEntities.NextPageToken != null);
    }

    private async Task AddCasesToSearchIndex()
    {
        PagedResult<CaseEntity>? caseEntities = null;
        do
        {
            _logger.LogInformation("Querying for Cases that can be added to Coveo search index.");
            caseEntities = await _caseRepository.GetCasesReadyForSearch(
                _searchabilityOptions.SearchableGracePeriod,
                _searchabilityOptions.DatabaseQueryPageSize,
                caseEntities?.NextPageToken);
            _logger.LogInformation("Found {count} cases that can be added to " +
                                   "Coveo search index. Has next page: {hasNextPage}",
                                    caseEntities.Items.Count, caseEntities.NextPageToken != null);
            foreach (var caseEntity in caseEntities.Items)
            {
                await UpdateCaseSearchability(caseEntity, true);
            }
        } while (caseEntities.NextPageToken != null);
    }

    private async Task RemoveCasesFromConfidentialGracePeriod()
    {
        PagedResult<CaseEntity>? caseEntities = null;
        do
        {
            _logger.LogInformation("Querying for Cases after confidential grace period.");
            caseEntities = await _caseRepository.GetCasesAfterConfidentialGracePeriod(
                _searchabilityOptions.ConfidentialGracePeriod,
                _searchabilityOptions.DatabaseQueryPageSize,
                caseEntities?.NextPageToken);
            _logger.LogInformation("Found {count} cases after confidential " +
                                   "grace period. Has next page: {hasNextPage}",
                caseEntities.Items.Count, caseEntities.NextPageToken != null);
            foreach (var caseEntity in caseEntities.Items)
            {
                await UpdateCaseConfidentiality(caseEntity);
            }
        } while (caseEntities.NextPageToken != null);
    }

    private async Task UpdateCaseSearchability(CaseEntity caseEntity, bool isSearchable)
    {
        _logger.LogInformation("Changing Case {caseKey}/{caseId} searchable state to: {isSearchable} for Search index.",
            caseEntity.Key, caseEntity.Id, isSearchable);

        await _singleCaseRepository.PatchCase(
            caseEntity.Id,
            caseEntity.Key,
            new Dictionary<string, object?>
            {
                { nameof(CaseEntity.IsSearchable).ToCamelCase(), isSearchable },
                { nameof(CaseEntity.ModifiedBy).ToCamelCase(), SearchableCasesCrawlerUserInfo },
                { nameof(CaseEntity.Modified).ToCamelCase(), DateTime.UtcNow },
                { nameof(CaseEntity.CorrelationId).ToCamelCase(), caseEntity.CorrelationId }
            });
    }

    private async Task UpdateCaseConfidentiality(CaseEntity caseEntity)
    {
        _logger.LogInformation("Removing Case {caseKey}/{caseId} from confidential grace period.",
                                caseEntity.Key, caseEntity.Id);
        await _singleCaseRepository.PatchCase(
            caseEntity.Id,
            caseEntity.Key,
            new Dictionary<string, object?>
            {
                { nameof(CaseEntity.IsInConfidentialGracePeriod).ToCamelCase(), false },
                { nameof(CaseEntity.ModifiedBy).ToCamelCase(), SearchableCasesCrawlerUserInfo },
                { nameof(CaseEntity.Modified).ToCamelCase(), DateTime.UtcNow },
                { nameof(CaseEntity.CorrelationId).ToCamelCase(), caseEntity.CorrelationId }
            });
    }
}