using PEXC.Common.Options;

namespace PEXC.Case.Services.Infrastructure;

public class StaffingApiOptions : OptionsBase, IRetryPolicyOptions
{
    public string? BaseAddress { get; set; }
    public string? ApiKey { get; set; }
    public int MaxRetryCount { get; set; } = 3;
    public int RetryBaseBackoffMs { get; set; } = 100;
    public int ResourceAllocationRequestChunkSize { get; set; } = 10;
    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromHours(24);

    public override IList<OptionsValidationError> Validate()
    {
        return new OptionsValidator<StaffingApiOptions>(this)
            .IsUrl(t => t.BaseAddress)
            .IsEqualOrGreaterThan(x => x.ResourceAllocationRequestChunkSize, 1)
            .NotEmpty(t => t.ApiKey)
            .Validate();
    }
}