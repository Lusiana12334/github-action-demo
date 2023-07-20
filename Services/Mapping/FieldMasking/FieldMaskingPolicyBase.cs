using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;

namespace PEXC.Case.Services.Mapping.FieldMasking;

public abstract class FieldMaskingPolicyBase : IFieldMaskingPolicy
{
    protected readonly string _placeholder;

    protected FieldMaskingPolicyBase(string placeholder) => _placeholder = placeholder;

    public void Apply(CaseEntity source, CaseSearchItemDto destination)
    {
        if (ShouldApply(source))
            ApplyMasking(source, destination);
    }

    protected abstract bool ShouldApply(CaseEntity source);

    protected abstract void ApplyMasking(CaseEntity source, CaseSearchItemDto destination);

    protected void ApplyDefaultFieldsMasking(CaseSearchItemDto destination)
    {
        destination.ClientName = _placeholder;
        destination.TargetName = _placeholder;
        destination.TargetDescription = _placeholder;
        destination.MainCompetitorsAnalyzed = _placeholder;
        destination.FinalMaterialAvailable = _placeholder;
        destination.ManagerName = _placeholder;
        destination.AdvisorsNames = _placeholder;
        destination.Keyword = _placeholder;
        destination.IndustrySectorsAnalyzed = _placeholder;
        destination.GeographicRegion = _placeholder;
        destination.AdvancedAnalyticsUsage = _placeholder;
        destination.OpsDdComponent = _placeholder;
        destination.OpsDdDuration = _placeholder;
        destination.OpsDdTeam = _placeholder;
    }
}