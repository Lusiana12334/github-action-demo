using FluentValidation;
using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;

namespace PEXC.Case.Functions.Validators;

public class ScheduleSurveyValidator : AbstractValidator<AsbMessageDto>
{
    public ScheduleSurveyValidator()
    {
        RuleFor(e => e.CorrelationId).NotEmpty();
        Transform(x => x.Entity, e => e as CaseEntity)
            .NotNull()
            .ChildRules(r =>
            {
                r.RuleFor(ce => ce!.EndDate).NotNull();
                r.RuleFor(ce => ce!.Timestamp).GreaterThan(0);
                r.RuleFor(ce => ce!.ETag).NotEmpty();
            });
    }
}