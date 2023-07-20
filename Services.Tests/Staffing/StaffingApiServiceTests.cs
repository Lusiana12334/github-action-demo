using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using PEXC.Case.Services.Staffing;
using PEXC.Case.Services.Staffing.Contracts;
using PEXC.Case.Tests.Common;

namespace PEXC.Case.Services.Tests.Staffing;

public class StaffingApiServiceTests
{
    [Fact]
    public async Task GetResourceAllocations_WhenStatusSuccess_ReturnsExpectedItems()
    {
        // Arrange
        var caseCode = "ABC123";
        var expectedResponse = new[]
        {
            new ResourceAllocation { OldCaseCode = caseCode, EmployeeCode = "EmpCode01", CaseRoleCode = StaffingApiService.OperatingPartnerRoleCode },
            new ResourceAllocation { OldCaseCode = caseCode, EmployeeCode = "EmpCode02", CaseRoleCode = StaffingApiService.OperatingPartnerRoleCode },
            new ResourceAllocation { OldCaseCode = caseCode, EmployeeCode = "EmpCode01 ", CaseRoleCode = StaffingApiService.OperatingPartnerRoleCode },
            new ResourceAllocation { OldCaseCode = caseCode, EmployeeCode = "Adv01", CaseRoleCode = StaffingApiService.AdvisorRoleCode },
            new ResourceAllocation { OldCaseCode = caseCode, EmployeeCode = "Adv02", CaseRoleCode = StaffingApiService.AdvisorRoleCode },
            new ResourceAllocation { OldCaseCode = caseCode, EmployeeCode = "Adv02 ", CaseRoleCode = StaffingApiService.AdvisorRoleCode },
        };

        var httpClient = MockHttpClient(
            r => r.RequestUri!.LocalPath == "/resourceAllocation/allocationsBySelectedValues"
                    && r.Content!.ReadFromJsonAsync<GetResourceAllocationsRequest>().Result!.OldCaseCodes == caseCode,
            expectedResponse);
        var service = CreateService(httpClient);

        // Act
        var allocations = await service.GetCasesTeamMembers(new[] { caseCode });

        // Assert
        allocations
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, CaseTeamMembers>
                {
                    { caseCode, new CaseTeamMembers(
                        new List<string> { "EmpCode01", "EmpCode02"}, 
                        new List<string> { "Adv01", "Adv02"}) }
                });
    }

    [Fact]
    public void GetResourceAllocations_WhenStatusNotSuccess_ThrowsException()
    {
        // Arrange
        var httpClient = MockHttpClient(
            r => r.RequestUri!.LocalPath == "/resourceAllocation/allocationsBySelectedValues",
            Array.Empty<ResourceAllocation>(),
            HttpStatusCode.BadRequest);

        var service = CreateService(httpClient);

        // Act
        var call = () => service.GetCasesTeamMembers(new[] { "ABC123" });

        // Assert
        call.Should().ThrowAsync<HttpRequestException>();
    }

    private static StaffingApiService CreateService(HttpClient httpClient)
    {
        return new StaffingApiService(httpClient, Substitute.For<ILogger<StaffingApiService>>(), 10);
    }

    private static HttpClient MockHttpClient(
        Predicate<HttpRequestMessage> requestPredicate,
        IEnumerable<ResourceAllocation> expectedResponse,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var httpMessageHandler = new SimpleMockHttpMessageHandler(expectedResponse, statusCode, requestPredicate);
        return new HttpClient(httpMessageHandler)
        {
            BaseAddress = new Uri("https://some.base.address.com/api")
        };
    }
}