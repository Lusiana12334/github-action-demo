using PEXC.Common.Options;

namespace PEXC.Case.Services.Infrastructure;

public class ProfileApiOptions : OptionsBase
{
    public string BaseUrl { get; set; } = null!;
    public string Scope { get; set; } = null!;
    public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromHours(1);

    public override IList<OptionsValidationError> Validate()
    {
        return new OptionsValidator<ProfileApiOptions>(this)
            .IsUrl(x => x.BaseUrl)
            .NotEmpty(x => x.Scope)
            .Validate();
    }
}