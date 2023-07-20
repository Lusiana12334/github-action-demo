using PEXC.Common.Options;

namespace PEXC.Case.Tools.Migration;

public class MigrationOptions : OptionsBase
{
    public bool RestoreSurveyOpeningToNew { get; set; }

    public bool RandomizeData { get; set; }

    public bool CreateDirectories { get; set; } = true;

    public DateTime OpenedSurveyArchiveDate { get; set; } = DateTime.MinValue;

    public override  IList<OptionsValidationError> Validate() 
        => Array.Empty<OptionsValidationError>();
}