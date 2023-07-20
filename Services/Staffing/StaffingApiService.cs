using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PEXC.Case.Infrastructure;
using PEXC.Case.Services.Staffing.Contracts;
using PEXC.Common.Logging.Extensions;

namespace PEXC.Case.Services.Staffing;

internal class StaffingApiService : IStaffingApiService
{
    internal const string OperatingPartnerRoleCode = "OVP";

    internal const string AdvisorRoleCode = "AD";

    private readonly HttpClient _httpClient;

    private readonly ILogger<StaffingApiService> _logger;

    private readonly int _resourceAllocationRequestChunkSize;

    public StaffingApiService(HttpClient httpClient, ILogger<StaffingApiService> logger, int resourceAllocationRequestChunkSize)
    {
        _httpClient = httpClient;
        _logger = logger;
        _resourceAllocationRequestChunkSize = resourceAllocationRequestChunkSize;
    }

    public async Task<IReadOnlyDictionary<string, CaseTeamMembers>> GetCasesTeamMembers(IReadOnlyCollection<string> caseCodes)
    {
        var roleCodes = OperatingPartnerRoleCode + "," + AdvisorRoleCode;
        var resourceAllocations = new List<ResourceAllocation>();

        foreach (var caseCodesChunk in caseCodes.Chunk(_resourceAllocationRequestChunkSize))
        {
            var request = new GetResourceAllocationsRequest
            {
                OldCaseCodes = string.Join(',', caseCodesChunk),
                CaseRoleCodes = roleCodes, 
            };

            resourceAllocations.AddRange(await GetResourceAllocations(request));
        }

        return resourceAllocations
            .GroupBy(ra => ra.OldCaseCode!)
            .ToDictionary(g => g.Key, SplitToCaseRoles);
    }

    private static CaseTeamMembers SplitToCaseRoles(IEnumerable<ResourceAllocation> allocations)
    {
        var advisors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var operatingPartners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var allocation in allocations)
        {
            if (string.Equals(allocation.CaseRoleCode, OperatingPartnerRoleCode, StringComparison.OrdinalIgnoreCase))
                operatingPartners.Add(allocation.EmployeeCode!.Trim());
            
            if (string.Equals(allocation.CaseRoleCode, AdvisorRoleCode, StringComparison.OrdinalIgnoreCase))
                advisors.Add(allocation.EmployeeCode!.Trim());
        }

        return new CaseTeamMembers(operatingPartners.ToList(), advisors.ToList());
    }

    private async Task<IEnumerable<ResourceAllocation>> GetResourceAllocations(GetResourceAllocationsRequest request)
    {
        using (_logger.StopwatchBlock(
                   $"Querying Staffing API for resource allocations filtered by: {JsonConvert.SerializeObject(request)}",
                   "Staffing API query finished."))
        {
            return await _httpClient
                       .PostAsync<GetResourceAllocationsRequest, IEnumerable<ResourceAllocation>>(
                           "resourceAllocation/allocationsBySelectedValues", request)
                   ?? Array.Empty<ResourceAllocation>();
        }
    }
}