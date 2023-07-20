using System.Text.Json.Serialization;

namespace PEXC.Case.Services.Staffing.Contracts;

public record GetResourceAllocationsRequest
{
    public string? OldCaseCodes { get; set; }
    public string? EmployeeCodes { get; set; }
    public DateTime? LastUpdated { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? CaseRoleCodes { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ResourceAllocationAction Action { get; set; } = ResourceAllocationAction.Upserted;
}