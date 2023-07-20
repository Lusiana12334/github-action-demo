using AutoMapper;
using Microsoft.Extensions.Logging;
using PEXC.Case.DataAccess;
using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;

namespace PEXC.Case.Services;

public class CoveoCaseService : ICoveoCaseService
{
    private readonly ICaseRepository _caseRepository;

    private readonly IMapper _mapper;

    private readonly ILogger<CoveoCaseService> _logger;

    public CoveoCaseService(ICaseRepository caseRepository, IMapper mapper, ILogger<CoveoCaseService> logger)
    {
        _caseRepository = caseRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResult<CaseSearchItemDto>> GetSearchableCases(
        PaginationRequestDto paginationData)
    {
        _logger.LogInformation(
            "Querying for Coveo searchable cases with: {paginationData}",
            paginationData);

        var caseEntities = await _caseRepository.GetSearchableCases(
            paginationData.ModifiedSince, paginationData.PageSize, paginationData.NextPageToken);
        _logger.LogInformation("Received {count} Coveo searchable cases for indexing", caseEntities.Items.Count);

        return _mapper.Map<PagedResult<CaseSearchItemDto>>(caseEntities);
    }

    public async Task<PagedResult<CaseManagementItemDto>> GetActiveCases(
        PaginationRequestDto paginationData)
    {
        _logger.LogInformation(
            "Querying for Coveo active cases with: {paginationData}",
            paginationData);

        var caseEntities = await _caseRepository.GetActiveCases(
            paginationData.ModifiedSince, paginationData.PageSize, paginationData.NextPageToken);
        _logger.LogInformation("Received {count} Coveo active cases for indexing", caseEntities.Items.Count);

        return _mapper.Map<PagedResult<CaseManagementItemDto>>(caseEntities);
    }

}