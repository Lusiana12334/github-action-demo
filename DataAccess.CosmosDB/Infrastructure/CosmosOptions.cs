using PEXC.Common.Options;

namespace PEXC.Case.DataAccess.CosmosDB.Infrastructure;

public class CosmosOptions : OptionsBase
{
    public string ConnectionString { get; set; } = null!;
    public string Database { get; set; } = null!;
    public string Container { get; set; } = null!;
    public string LeasesContainer { get; set; } = null!;
    public string AuditContainer { get; set; } = null!;
    public bool CreateDatabase { get; set; }
    public int MaxRetryCount { get; set; } = 3;
    public int RetryBaseBackoffMs { get; set; } = 100;
    public bool PopulateIndexMetrics { get; set; } = false;

    public override IList<OptionsValidationError> Validate()
    {
        return new OptionsValidator<CosmosOptions>(this)
            .NotEmpty(x => x.ConnectionString)
            .NotEmpty(x => x.LeasesContainer)
            .NotEmpty(x => x.AuditContainer)
            .NotEmpty(x => x.Database)
            .NotEmpty(x => x.Container)
            .Validate();
    }
}