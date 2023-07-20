using PEXC.Case.Services.IRIS.Contracts;

namespace PEXC.Case.Services.IRIS;

public interface IIrisApiService
{
    Task<ApiResult<IrisCaseDto>> GetCases(int pageNumber, int pageCount, SearchCasesDto filter);
}