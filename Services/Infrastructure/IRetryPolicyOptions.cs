namespace PEXC.Case.Services.Infrastructure;

public interface IRetryPolicyOptions
{
    int MaxRetryCount { get; set; }
    int RetryBaseBackoffMs { get; set; }
}