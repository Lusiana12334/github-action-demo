using System.Runtime.Serialization;

namespace PEXC.Case.Domain;

public enum FinalMaterialAvailable
{
    [EnumMember(Value = "N - Active Case")]
    NoActiveCase = 0,
    [EnumMember(Value = "N - Pending Submission")]
    NoPendingSubmission,
    [EnumMember(Value = "Y")]
    Yes,
    [EnumMember(Value = "N/A - CDD")]
    NACdd,
    [EnumMember(Value = "N/A - Duplicate Case")]
    NADuplicateCase,
    [EnumMember(Value = "N/A - Incomplete Case")]
    NAIncompleteCase,
    [EnumMember(Value = "N/A - Confidential Case")]
    NAConfidentialCase
}