using FluentValidation;
using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;
using PEXC.Case.Services;

namespace PEXC.Case.Functions.Validators;

public class UpdateSurveyValidator : AbstractValidator<AsbMessageDto>
{
    public UpdateSurveyValidator(IProfileMapper profileMapper, string correlationId)
    {
        RuleFor(e => e.CorrelationId).NotEmpty();
        Transform(x => x.Entity, e => e as CaseEntity)
            .NotNull()
            .SetValidator(new CaseEntityValidator(profileMapper, correlationId));
    }

    private class CaseEntityValidator : AbstractValidator<CaseEntity?>
    {
        private const string CannotUpdatePermissionMessageTemplate =
            "Cannot change permission on SP directory because {0} was not provided.";

        private const string ProfileNotFoundMessageTemplate =
            "Cannot change permission on SP directory because the employee profile for {0} was not found" +
            " or the user is no longer an active employee. EmployeeCode:[{1}]";

        public CaseEntityValidator(IProfileMapper profileMapper, string correlationId)
        {
            RuleFor(e => e!.ItemStage)
                .Must(c => c is CaseState.SurveyOpened or CaseState.Deleted);

            RuleFor(e => e!.ManagerEcode)
                .NotEmpty()
                .WithMessage(string.Format(CannotUpdatePermissionMessageTemplate, nameof(CaseEntity.ManagerEcode)))
                .SetAsyncValidator(
                    new UserProfilePropertyValidator(
                        profileMapper,
                        nameof(CaseEntity.ManagerEcode),
                        correlationId,
                        true)!)
                .WithMessage(e =>
                    string.Format(ProfileNotFoundMessageTemplate, nameof(CaseEntity.ManagerEcode), e!.ManagerEcode));

            RuleFor(e => e!.SharePointDirectory)
                .NotNull()
                .SetValidator(new SharePointEntityValidator(CannotUpdatePermissionMessageTemplate));
        }
    }
}