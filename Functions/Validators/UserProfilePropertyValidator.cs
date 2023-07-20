using FluentValidation.Validators;
using FluentValidation;
using PEXC.Case.Domain;
using PEXC.Case.Services;

namespace PEXC.Case.Functions.Validators;

public class UserProfilePropertyValidator : AsyncPropertyValidator<CaseEntity, string?>
{
    private readonly IProfileMapper _profileMapper;
    private readonly string _correlationId;
    private readonly bool _onlyActive;
    public override string Name { get; }

    public UserProfilePropertyValidator(IProfileMapper profileMapper, string propertyName, string correlationId, bool onlyActive)
    {
        _profileMapper = profileMapper;
        _correlationId = correlationId;
        _onlyActive = onlyActive;
        Name = propertyName;
    }

    public override async Task<bool> IsValidAsync(ValidationContext<CaseEntity> context, string? value, CancellationToken cancellation)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        var userProfiles =
            await _profileMapper.GetEmployeeProfiles(new[] { value }, _correlationId);

        return userProfiles.ContainsKey(value) && (!_onlyActive || !userProfiles[value].IsTerminated);
    }
}