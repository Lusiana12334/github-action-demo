using System.Runtime.Serialization;

namespace PEXC.Case.Tools.Migration;

#pragma warning disable CS8618
public enum MigrationCaseState
{
    [EnumMember(Value = "Active")]
    New = 0,
    [EnumMember(Value = "Closed-Email2CM")]
    SurveyOpening,
    [EnumMember(Value = "Closed-PendingData")]
    SurveyOpened,
    [EnumMember(Value = "Closed-DataRecorded")]
    SurveyClosing,
    [EnumMember(Value = "Closed-KMNotified")]
    SurveyClosed,
    [EnumMember(Value = "Closed-Final")]
    Published
}