using PEXC.Case.DataContracts.V1.CaseForms;

namespace PEXC.Case.Services;

public interface ISurveyService
{
    Task<SurveyDto> GetSurvey(string id, string key);
    public Task<bool> SaveSurvey(SurveyDto updateSurveyDto);
    public Task<bool> SubmitSurvey(SurveyDto submitSurveyDto);
}