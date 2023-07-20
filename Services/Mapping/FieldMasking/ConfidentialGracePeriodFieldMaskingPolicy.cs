using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;

namespace PEXC.Case.Services.Mapping.FieldMasking;

public class ConfidentialGracePeriodFieldMaskingPolicy : FieldMaskingPolicyBase
{
    public ConfidentialGracePeriodFieldMaskingPolicy(string placeholder)
        : base(placeholder)
    { }

    protected override bool ShouldApply(CaseEntity source)
        => source.IsInConfidentialGracePeriod != false;

    protected override void ApplyMasking(CaseEntity source, CaseSearchItemDto destination)
    {
        destination.TargetName = _placeholder;
    }
}