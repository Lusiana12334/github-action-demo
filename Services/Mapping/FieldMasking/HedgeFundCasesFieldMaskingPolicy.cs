using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;

namespace PEXC.Case.Services.Mapping.FieldMasking;

public class HedgeFundCasesFieldMaskingPolicy : FieldMaskingPolicyBase
{
    private readonly int[] _confidentialIndustries;

    public HedgeFundCasesFieldMaskingPolicy(string placeholder, int[] confidentialIndustries)
        : base(placeholder)
    {
        _confidentialIndustries = confidentialIndustries;
    }

    protected override bool ShouldApply(CaseEntity source)
        => source.PrimaryIndustry?.Id != null && _confidentialIndustries.Contains(source.PrimaryIndustry.Id.Value);

    protected override void ApplyMasking(CaseEntity source, CaseSearchItemDto destination) 
        => ApplyDefaultFieldsMasking(destination);
}