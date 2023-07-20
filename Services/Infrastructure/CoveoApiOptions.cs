using PEXC.Common.Options;

namespace PEXC.Case.Services.Infrastructure;

public class CoveoApiOptions : OptionsBase
{
    public string CaseSearchApiKey { get; set; } = null!;
    public string CaseSearchRefreshApiKey { get; set; } = null!;
    public string CaseManagementApiKey { get; set; } = null!;
    public string CaseManagementRefreshApiKey { get; set; } = null!;
    public string Endpoint { get; set; } = null!;
    public string Provider { get; set; } = null!;
    public string OrganizationId { get; set; } = null!;
    public string CaseSearchSourceId { get; set; } = null!;
    public string CaseManagementSourceId { get; set; } = null!;
    
    //Cannot be longer than LockTime set on Subscription 
    public int MinSearchRefreshDebounceInSeconds { get; set; } = 30;

    public override IList<OptionsValidationError> Validate()
    {
        return new OptionsValidator<CoveoApiOptions>(this)
            .NotEmpty(x => x.CaseSearchApiKey)
            .NotEmpty(x => x.CaseSearchRefreshApiKey)
            .NotEmpty(x => x.CaseManagementApiKey)
            .NotEmpty(x => x.CaseManagementRefreshApiKey)
            .IsUrl(x => x.Endpoint)
            .NotEmpty(x => x.Provider)
            .NotEmpty(x => x.OrganizationId)
            .NotEmpty(x => x.CaseSearchSourceId)
            .NotEmpty(x => x.CaseManagementSourceId)
            .Validate();
    }
}