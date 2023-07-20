using PEXC.Common.Options;

namespace PEXC.Case.Services.Infrastructure;

public class ClientCaseApiOptions : OptionsBase, IRetryPolicyOptions
{
    public string? BaseAddress { get; set; }
    public string? ApiKey { get; set; }
    public int MaxRetryCount { get; set; } = 3;
    public int RetryBaseBackoffMs { get; set; } = 100;

    public override IList<OptionsValidationError> Validate()
    {
        return new OptionsValidator<ClientCaseApiOptions>(this)
            .IsUrl(t => t.BaseAddress)
            .NotEmpty(t => t.ApiKey)
            .Validate();
    }
}