using System.Data;
using FluentValidation;
using PEXC.Case.DataAccess;
using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;

namespace PEXC.Case.Functions.Validators;

public class TriggerSurveyValidator : AbstractValidator<AsbMessageDto>
{
    public TriggerSurveyValidator(ISingleCaseRepository singleCaseRepository)
    {
        RuleFor(e => e.CorrelationId).NotEmpty();
        Transform(x => x.Entity, entity => entity as CaseEntity)
            .NotNull()
            .ChildRules(r =>
                r.RuleFor(e => e)
                    .MustAsync(async (e, _) =>
                    {
                        var currentCase = await singleCaseRepository.GetCase(e!.Id, e.Key);
                        return currentCase != null;
                    }).WithMessage("Case entity does not exist!")
                    .MustAsync(async (e, _) =>
                    {
                        var currentCase = await singleCaseRepository.GetCase(e!.Id, e.Key);
                        return currentCase?.EndDate != null;
                    }).WithMessage("Case entity does not have end date!")
                    .MustAsync(async (e, _) =>
                    {
                        var currentCase = await singleCaseRepository.GetCase(e!.Id, e.Key);
                        return currentCase?.Timestamp == e.Timestamp && currentCase.ETag == e.ETag;
                    }).WithMessage("Case entity has been changed since message was scheduled, skipping execution").WithSeverity(Severity.Info)
                );
    }
}