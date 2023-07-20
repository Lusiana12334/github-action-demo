using PEXC.Common.Options;

namespace PEXC.Case.Services.Infrastructure;

public class CaseDataImportOptions : OptionsBase
{
    public DateOnly InitialModifiedAfterTime { get; set; } = DateOnly.MinValue;
    public int[] PegIndustries { get; set; } = Array.Empty<int>();
    public int[] PegCapabilities { get; set; } = Array.Empty<int>();
    public string CCMTimeZone { get; set; } = "Eastern Standard Time";
    public DateTime MinDate { get; set; } = new(1970, 1, 1);
    public DateTime MaxDate { get; set; } = new(2039, 12, 31);

    public override IList<OptionsValidationError> Validate() 
        => Array.Empty<OptionsValidationError>();
}