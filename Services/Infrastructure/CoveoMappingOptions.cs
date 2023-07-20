using PEXC.Common.Options;

namespace PEXC.Case.Services.Infrastructure;

public class CoveoMappingOptions : OptionsBase
{
    public IDictionary<string, IEnumerable<int> > CaseTypeClusteredCapability { get; set; } = new Dictionary<string, IEnumerable<int>>();

    public override IList<OptionsValidationError> Validate()
    {
        return Array.Empty<OptionsValidationError>();
    }
}