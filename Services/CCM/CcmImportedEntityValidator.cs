using FluentValidation;
using PEXC.Case.Domain;
using PEXC.Case.Services.Infrastructure;

namespace PEXC.Case.Services.CCM;

public class CcmImportedEntityValidator : AbstractValidator<CaseEntity>
{
    public CcmImportedEntityValidator(CaseDataImportOptions options)
    {
        RuleFor(e => e.CaseCode).NotEmpty().WithSeverity(Severity.Error);
        RuleFor(e => e.CaseName).NotEmpty().WithSeverity(Severity.Error);

        RuleFor(e => e.PrimaryIndustry).NotEmpty().WithSeverity(Severity.Error);
        When(
            e => e.PrimaryIndustry != null,
            () => RuleFor(e => e.PrimaryIndustry!.Id)
                .NotEmpty()
                .WithSeverity(Severity.Error));

        RuleFor(e => e.PrimaryCapability).NotEmpty().WithSeverity(Severity.Error);
        When(
            e => e.PrimaryCapability != null,
            () => RuleFor(e => e.PrimaryCapability!.Id)
                .NotEmpty()
                .WithSeverity(Severity.Error));

        When(
            e => e.PrimaryIndustry is { Id: { } },
            () =>
            {
                RuleFor(x => x.PrimaryIndustry)
                    .Must(x => options.PegIndustries.Contains(x!.Id!.Value))
                    .WithMessage("Case does not belong to PEG industry and will be skipped")
                    .WithSeverity(Severity.Info);
            });

        When(
            e => e.PrimaryCapability is { Id: { } },
            () =>
            {
                RuleFor(x => x.PrimaryCapability)
                    .Must(x => options.PegCapabilities.Contains(x!.Id!.Value))
                    .WithMessage("Case does not belong to PEG capabilities and will be skipped")
                    .WithSeverity(Severity.Info);
            });
    }
}