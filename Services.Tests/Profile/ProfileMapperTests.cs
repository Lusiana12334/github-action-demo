using PEXC.Case.Tests.Common;
using PEXC.Common.BaseApi.Profile;

namespace PEXC.Case.Services.Tests.Profile;

public class ProfileMapperTests
{
    [Fact]
    public async Task GetEmployeeProfiles()
    {
        // Arrange
        var repository = Substitute.For<IProfileRepository>();
        var ecodes = new List<string> { "eCode2", "eCode4", "eCode7" };
        repository
            .GetProfiles(Arg.Is<IReadOnlyList<string>>(list => list.OrderBy(x => x).SequenceEqual(ecodes)), "correlationId")
            .Returns(ecodes.ConvertAll(Fake.EmployeeDetails));

        var mapper = new ProfileMapper(repository);

        // Act
        var employeeProfiles =  await mapper.GetEmployeeProfiles(ecodes, "correlationId");

        // Assert
        employeeProfiles.Count.Should().Be(ecodes.Count);
    }
}