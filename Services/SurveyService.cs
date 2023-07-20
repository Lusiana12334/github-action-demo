using Microsoft.Extensions.Logging;
using PEXC.Case.DataAccess;
using AutoMapper;
using PEXC.Case.Domain;
using PEXC.Common.BaseApi.ErrorHandling;
using PEXC.Common.BaseApi.User;
using PEXC.Case.DataContracts.V1.CaseForms;
using PEXC.Common.Logging.Extensions;

namespace PEXC.Case.Services;

public class SurveyService : ISurveyService
{
    private readonly ISingleCaseRepository _caseRepository;
    private readonly IMapper _mapper;
    private readonly IUserProvider _userProvider;
    private readonly ILogger<SurveyService> _logger;

    public SurveyService(
        ISingleCaseRepository caseRepository,
        IMapper mapper,
        IUserProvider userProvider,
        ILogger<SurveyService> logger)
    {
        _caseRepository = caseRepository;
        _mapper = mapper;
        _userProvider = userProvider;
        _logger = logger;
    }

    public async Task<SurveyDto> GetSurvey(string id, string key)
    {
        var caseEntity = await ValidateAccessAndReturnCaseEntity(id, key);
        var dto = _mapper.Map<SurveyDto>(caseEntity);

        return dto;
    }

    public async Task<bool> SaveSurvey(SurveyDto updateSurveyDto)
    {
        var originalCaseEntity = await ValidateAccessAndReturnCaseEntity(updateSurveyDto.Id, updateSurveyDto.Key);
        _logger.LogInformation($"Saving survey. {LoggerConsts.CaseCodeProperty}:{{{LoggerConsts.CaseCodeProperty}}}  " +
                               $"{LoggerConsts.CorrelationIdProperty}:{{{LoggerConsts.CorrelationIdProperty}}}",
            originalCaseEntity.CaseCode, originalCaseEntity.CorrelationId);
        var resultCase = await ApplySurveyOnCaseEntity(updateSurveyDto, originalCaseEntity);
        return await _caseRepository.UpdateCase(resultCase);
    }

    public async Task<bool> SubmitSurvey(SurveyDto submitSurveyDto)
    {
        var originalCaseEntity = await ValidateAccessAndReturnCaseEntity(submitSurveyDto.Id, submitSurveyDto.Key);
        _logger.LogInformation(
            $"Submitting survey. {LoggerConsts.CaseCodeProperty}:{{{LoggerConsts.CaseCodeProperty}}} " +
            $"{LoggerConsts.CorrelationIdProperty}:{{{LoggerConsts.CorrelationIdProperty}}}",
            originalCaseEntity.CaseCode, originalCaseEntity.CorrelationId);
        var resultCase = await ApplySurveyOnCaseEntity(submitSurveyDto, originalCaseEntity);
        resultCase.ItemStage = CaseState.SurveyClosing;
        return await _caseRepository.UpdateCase(resultCase);
    }

    private async Task<CaseEntity> ValidateAccessAndReturnCaseEntity(string id, string key)
    {
        var currentUser = await GetCurrentUserInfo();
        var caseEntity = await GetOriginalCaseEntity(id, key);

        var surveyOpened = caseEntity.ItemStage == CaseState.SurveyOpened;
        var currentUserIsCaseManager = string.Equals(caseEntity.ManagerEcode,
            currentUser.UserEcode, StringComparison.OrdinalIgnoreCase);

        var messages = new List<string>();

        if (!surveyOpened)
            messages.Add("the survey is not open");
        if (!currentUserIsCaseManager)
            messages.Add("current user is not a case manager");

        if (messages.Count > 0)
        {
            _logger.LogWarning("No access - {reasons} / {caseManagerEcode} / {currentUserEcode} ",
                string.Join(", ", messages), caseEntity.ManagerEcode, currentUser.UserEcode);

            throw new NotFoundException($"Not found for the user {currentUser.UserEcode}");
        }

        return caseEntity;
    }

    private async Task<CaseEntity> GetOriginalCaseEntity(string id, string key)
    {
        var originalCaseEntity = await _caseRepository.GetCase(id, key);
        if (originalCaseEntity == null || originalCaseEntity.IsDeleted)
        {
            throw new NotFoundException(
                $"Cannot save survey with id: {id} and key: {key}. " +
                "The original case entity does not exist in database.");
        }

        return originalCaseEntity;
    }

    private async Task<CaseEntity> ApplySurveyOnCaseEntity(SurveyDto updateSurveyDto, CaseEntity originalCaseEntity)
    {
        var resultCase = _mapper.Map(updateSurveyDto.SurveyTargetDetailsSection!, originalCaseEntity);
        resultCase.ModifiedBy = await GetCurrentUserInfo();
        resultCase.Modified = DateTime.UtcNow;
        return resultCase;
    }

    private async Task<UserInfo> GetCurrentUserInfo()
    {
        return new UserInfo(UserType.User, _userProvider.GetCurrentUserFullName())
        {
            UserEcode = await _userProvider.GetCurrentUserEcode(),
            UserId = _userProvider.GetCurrentUserId()
        };
    }
}