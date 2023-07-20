using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;

namespace PEXC.Case.Services.Mapping.FieldMasking;

public class SensitiveFieldMaskingPolicy : FieldMaskingPolicyBase
{
    public SensitiveFieldMaskingPolicy(string placeholder)
        : base(placeholder)
    { }

    protected override bool ShouldApply(CaseEntity source)
        => source.Sensitive is true;

    protected override void ApplyMasking(CaseEntity source, CaseSearchItemDto destination)
        => ApplyDefaultFieldsMasking(destination);
}