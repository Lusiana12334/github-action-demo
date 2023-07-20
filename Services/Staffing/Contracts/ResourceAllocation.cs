namespace PEXC.Case.Services.Staffing.Contracts;

public record ResourceAllocation
{
    public string? Action { get; set; }
    public int CaseCode { get; set; }
    public string? OldCaseCode { get; set; }
    public string? EmployeeCode { get; set; }
    public string? CaseRoleCode { get; set; }
}