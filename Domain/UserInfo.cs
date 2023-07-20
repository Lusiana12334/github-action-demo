namespace PEXC.Case.Domain;

public record UserInfo(UserType UserType, string DisplayName)
{
    public string? UserEcode { get; set; }
    public string? UserId { get; set; }
}