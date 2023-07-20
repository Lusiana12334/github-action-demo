using FluentValidation;
using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;
using PEXC.Case.Services;

namespace PEXC.Case.Functions.Validators;

public class StartSurveyValidator : AbstractValidator<AsbMessageDto>
{
    public StartSurveyValidator(IProfileMapper profileMapper, string correlationId)
    {
        RuleFor(e => e.CorrelationId).NotEmpty();
        Transform(x => x.Entity, e => e as CaseEntity)
            .NotNull()
            .SetValidator(new CaseEntityValidator(profileMapper, correlationId));
    }

    private class CaseEntityValidator : AbstractValidator<CaseEntity?>
    {
        private const string CannotStartSurveyMessageTemplate =
            "The survey cannot be started because {0} was not provided.";

        private const string ProfileNotFoundMessageTemplate =
            "The survey cannot be started because the employee profile for {0} was not found" +
            " or the user is no longer an active employee. EmployeeCode:[{1}]";

        public CaseEntityValidator(IProfileMapper profileMapper, string correlationId)
        {
            RuleFor(e => e!.ItemStage)
                .Equal(CaseState.New)
                .WithMessage("The survey cannot be started because ItemStage is not equal to 'New'");

            RuleFor(e => e!.EndDate)
                .NotEmpty()
                .WithMessage(string.Format(CannotStartSurveyMessageTemplate, nameof(CaseEntity.EndDate)));

            RuleFor(e => e!.CaseName)
                .NotEmpty()
                .WithMessage(
                    $"he survey cannot be started because {nameof(CaseEntity.CaseName)} was not provided. " +
                    $"{nameof(CaseEntity.CaseName)} is required to create a SharePoint directory");

            RuleFor(e => e!.UniqueId)
                .NotEmpty()
                .WithMessage(
                    $"The survey cannot be started because {nameof(CaseEntity.UniqueId)} was not provided. " +
                    $"{nameof(CaseEntity.UniqueId)} is required to create a SharePoint directory");

            RuleFor(e => e!.PrimaryCapability)
                .NotEmpty()
                .WithMessage(
                    $"The survey cannot be started because {nameof(CaseEntity.PrimaryCapability)} was not provided." +
                    $" {nameof(CaseEntity.PrimaryCapability)} is required to determine whether a case is confidential");

            RuleFor(e => e!.ManagerEcode)
                .NotEmpty()
                .WithMessage(string.Format(CannotStartSurveyMessageTemplate, nameof(CaseEntity.ManagerEcode)))
                .SetAsyncValidator(
                    new UserProfilePropertyValidator(
                        profileMapper,
                        nameof(CaseEntity.ManagerEcode),
                        correlationId,
                        true)!)
                .WithMessage(e =>
                    string.Format(ProfileNotFoundMessageTemplate, nameof(CaseEntity.ManagerEcode), e!.ManagerEcode));
        }
    }
}