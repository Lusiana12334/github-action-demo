using PEXC.Common.Options;

namespace PEXC.Case.Services.Infrastructure;

public class CaseSearchabilityOptions : OptionsBase
{
    public TimeSpan SearchableGracePeriod { get; set; } = TimeSpan.FromDays(180);
    public TimeSpan ConfidentialGracePeriod { get; set; } = TimeSpan.FromDays(365);
    public int[] ConfidentialIndustries { get; set; } = Array.Empty<int>();
    public string ConfidentialDataPlaceholder { get; set; } = "Legally cannot be disclosed";
    public int DatabaseQueryPageSize { get; set; } = 100;

    public override IList<OptionsValidationError> Validate()
    {
        return Array.Empty<OptionsValidationError>();
    }
}