using PEXC.Common.Options;

namespace PEXC.Case.Services.Infrastructure;

public class CosmosChangeFeedOptions : OptionsBase
{
    public string CaseChangeTopicName { get; set; } = null!;
    public string StartSurveySubscription { get; set; } = null!;
    public string EndSurveySubscription { get; set; } = null!;
    public string AuditSubscription { get; set; } = null!;
    public string UserEditSubscription { get; set; } = null!;
    public string SurveyOpenedSubscription { get; set; } = null!;
    public int MaxDeliveryCount { get; set; } = 10;

    public override IList<OptionsValidationError> Validate()
    {
        return new OptionsValidator<CosmosChangeFeedOptions>(this)
            .NotEmpty(x => x.CaseChangeTopicName)
            .NotEmpty(x => x.StartSurveySubscription)
            .NotEmpty(x => x.EndSurveySubscription)
            .NotEmpty(x => x.AuditSubscription)
            .NotEmpty(x => x.UserEditSubscription)
            .NotEmpty(x => x.SurveyOpenedSubscription)
            .Validate();
    }
}