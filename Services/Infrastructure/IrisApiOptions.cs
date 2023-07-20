using PEXC.Common.Options;

namespace PEXC.Case.Services.Infrastructure;

public class IrisApiOptions : OptionsBase, IRetryPolicyOptions
{
    public string BaseUrl { get; set; } = null!;
    public string ApiKey { get; set; } = null!;
    public int MaxRetryCount { get; set; } = 3;
    public int RetryBaseBackoffMs { get; set; } = 100;

    public override IList<OptionsValidationError> Validate()
    {
        return new OptionsValidator<IrisApiOptions>(this)
            .IsUrl(x => x.BaseUrl)
            .NotEmpty(x => x.ApiKey)
            .Validate();
    }
}