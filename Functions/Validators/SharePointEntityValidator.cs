using FluentValidation;
using PEXC.Case.Domain;

namespace PEXC.Case.Functions.Validators;

public class SharePointEntityValidator : AbstractValidator<SharePointDirectoryEntity?>
{
    public SharePointEntityValidator(string messageTemplate)
    {
        RuleFor(e => e!.DriveId)
            .NotEmpty()
            .WithMessage(string.Format(messageTemplate, nameof(SharePointDirectoryEntity.DriveId)));

        RuleFor(e => e!.DirectoryId)
            .NotEmpty()
            .WithMessage(string.Format(messageTemplate, nameof(SharePointDirectoryEntity.DirectoryId)));
    }
}