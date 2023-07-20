using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;

namespace PEXC.Case.Services.Mapping.FieldMasking;

public class TargetPubliclyTradedFieldMaskingPolicy : FieldMaskingPolicyBase
{
    public TargetPubliclyTradedFieldMaskingPolicy(string placeholder)
        : base(placeholder)
    { }
    
    protected override bool ShouldApply(CaseEntity source)
        => source.TargetPubliclyTraded.GetValueOrDefault();

    protected override void ApplyMasking(CaseEntity source, CaseSearchItemDto destination)
        => ApplyDefaultFieldsMasking(destination);
}