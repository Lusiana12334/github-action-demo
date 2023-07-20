using PEXC.Common.Options;

namespace PEXC.Case.Services.Infrastructure;

public class WorkflowSurveyOptions : OptionsBase
{
    public string TriggerSurveyQueue { get; set; } = null!;
    public int[] ConfidentialCapabilities { get; set; } = Array.Empty<int>();

    public override IList<OptionsValidationError> Validate()
    {
        return Array.Empty<OptionsValidationError>();
    }
}