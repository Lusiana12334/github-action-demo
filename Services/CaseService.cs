using AutoMapper;
using PEXC.Case.DataAccess;
using PEXC.Case.DataContracts.V1;
using PEXC.Case.DataContracts.V1.CaseForms;
using PEXC.Case.Domain;
using PEXC.Case.Services.Infrastructure;
using PEXC.Common.BaseApi.ErrorHandling;
using PEXC.Common.BaseApi.User;

namespace PEXC.Case.Services;

public class CaseService : ICaseService
{
    private readonly ISingleCaseRepository _caseRepository;
    private readonly IMapper _mapper;
    private readonly IUserProvider _userProvider;

    public CaseService(
        ISingleCaseRepository caseRepository,
        IMapper mapper,
        IUserProvider userProvider)
    {
        _caseRepository = caseRepository;
        _mapper = mapper;
        _userProvider = userProvider;
    }

    public async Task<CaseEditDto> GetCase(string id, string key)
    {
        var caseEntity = await _caseRepository.GetCase(id, key);

        if (caseEntity == null || caseEntity.IsDeleted)
        {
            throw new NotFoundException($"Case with id: {id} and key: {key} does not exist ");
        }

        return _mapper.Map<CaseEditDto>(caseEntity);
    }

    public async Task<CaseIdentifierDto> AddCase(CaseCreateDto caseCreateDto)
    {
        var newCase = _mapper.Map<CaseEntity>(caseCreateDto);

        var currentUserInfo = await GetCurrentUserInfo();
        newCase.CreatedBy = currentUserInfo;
        newCase.ModifiedBy = currentUserInfo;

        var addedCase = await _caseRepository.AddCase(newCase);

        return new CaseIdentifierDto(addedCase.Id, addedCase.Key);
    }

    public async Task<bool> UpdateCase(CaseEditDto updateCaseDto)
    {
        var originalCaseEntity = await _caseRepository.GetCase(updateCaseDto.Id, updateCaseDto.Key);

        if (originalCaseEntity == null || originalCaseEntity.IsDeleted)
        {
            throw new NotFoundException(
                $"Cannot update case with id: {updateCaseDto.Id} and key: {updateCaseDto.Key}. " +
                "The original entity does not exist in database.");
        }

        var resultCase = _mapper.Map(updateCaseDto.TargetDetailsSection!, originalCaseEntity);

        if (originalCaseEntity.RelationshipType == RelationshipType.Retainer)
        {
            resultCase = _mapper.Map(updateCaseDto.CaseDetailsSection!, resultCase);
            resultCase.CaseCode = updateCaseDto.CaseCode.Trim();
        }

        resultCase.ItemStage = GetCaseState(updateCaseDto.Published, originalCaseEntity);
        resultCase.ModifiedBy = await GetCurrentUserInfo();
        resultCase.Modified = DateTime.UtcNow;

        return await _caseRepository.UpdateCase(resultCase);
    }

    public async Task<bool> DeleteCase(string id, string key)
    {
        var caseEntity = await _caseRepository.GetCase(id, key);
        if (caseEntity == null || caseEntity.IsDeleted)
        {
            throw new NotFoundException($"Case with id: {id} and key: {key} does not exist.");
        }

        var propertiesToUpdate = new Dictionary<string, object?>
        {
            { nameof(CaseEntity.ItemStage).ToCamelCase(), CaseState.Deleted },
            { nameof(CaseEntity.ModifiedBy).ToCamelCase(), await GetCurrentUserInfo() },
            { nameof(CaseEntity.Modified).ToCamelCase(), DateTime.UtcNow }
        };
        return await _caseRepository.PatchCase(id, key, propertiesToUpdate);
    }

    public async Task<bool> IsCaseUnique(string caseCode, string caseName, string? currentCaseId = null)
    {
        var foundEntity = await _caseRepository.GetRetainerCaseByCaseCodeAndName(caseCode, caseName);
        return foundEntity == null || (currentCaseId != null && foundEntity.Id == currentCaseId);
    }
    
    private static CaseState GetCaseState(bool? published, CaseEntity originalCaseEntity)
    {
        return originalCaseEntity.ItemStage switch
        {
            CaseState.SurveyClosed when published is true => CaseState.Published,
            CaseState.Published when published is false => CaseState.SurveyClosed,
            _ when originalCaseEntity.ItemStage != CaseState.Published 
                   && originalCaseEntity.ItemStage != CaseState.SurveyClosed 
                   && published.HasValue
                => throw new InvalidOperationException($"Case cannot be set as published in the state {originalCaseEntity.ItemStage}"),
            _ => originalCaseEntity.ItemStage
        };
    }

    private async Task<UserInfo> GetCurrentUserInfo()
        => new(UserType.User, _userProvider.GetCurrentUserFullName())
        {
            UserEcode = await _userProvider.GetCurrentUserEcode(),
            UserId = _userProvider.GetCurrentUserId()
        };
}