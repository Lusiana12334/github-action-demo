using PEXC.Case.Domain;

namespace PEXC.Case.Services.Workflow;

public interface IWorkflowSurveyService
{
    Task<CaseEntity?> GetCase(string caseId, string key);
    Task TriggerSurvey(CaseEntity caseEntity, UserInfo serviceUserInfo, string correlationId);
    Task ScheduleSurvey(CaseEntity caseEntity, UserInfo serviceUserInfo, string correlationId);
    Task StartSurvey(CaseEntity caseEntity, UserInfo serviceUserInfo, string correlationId);
    Task CloseSurvey(CaseEntity caseEntity, UserInfo serviceUserInfo, string correlationId);
    Task UpdateSurvey(CaseEntity caseEntity, UserInfo serviceUserInfo, string correlationId);
}