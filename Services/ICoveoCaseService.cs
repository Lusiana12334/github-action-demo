using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;

namespace PEXC.Case.Services;

public interface ICoveoCaseService
{
    Task<PagedResult<CaseSearchItemDto>> GetSearchableCases(PaginationRequestDto paginationData);
    Task<PagedResult<CaseManagementItemDto>> GetActiveCases(PaginationRequestDto paginationData);
}