namespace PEXC.Case.Domain;

public enum CaseState
{
    New = 0,
    SurveyOpening,
    SurveyOpened,
    SurveyClosing,
    SurveyClosed,
    Published,
    SurveyOpenedArchive,
    Deleted,
    SurveyScheduled,
}