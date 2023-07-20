using System.Net;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;
using PEXC.Case.Services.Workflow;
using PEXC.Case.Tests.Common;
using PEXC.Common.BaseApi.Profile;
using PEXC.Document.Client;
using PEXC.Document.Client.Constants;
using PEXC.Document.DataContracts.V1;

namespace PEXC.Case.Services.Tests.Workflow;

public class PermissionServiceTests
{
    [Fact]
    public async Task GrantPermission_Success()
    {
        // Arrange
        var ecodes = new List<string> { "eCode2" };
        var profileMapper = GetProfileMapper_ReturningProfiles(ecodes, "correlationId");
        var documentService = Substitute.For<IDocumentServiceClient>();

        var expectedResult = new DirectoryPermissionDto("permId", "a@bain.com", PermissionScope.User);

        documentService
            .GrantDirectoryPermission(Arg.Any<GrantPermissionDto>(), Arg.Any<string>())
            .Returns(new[] { expectedResult });

        var service = GetService(documentService, profileMapper);

        // Act
        var permission = await service
            .GrantPermission(
                ecodes[0],
                "driveId",
                "directoryId",
                PermissionLevels.Edit,
                "correlationId");

        //Assert
        permission.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task GrantPermission_ProfileNotFound_ThrowException()
    {
        // Arrange
        var ecodes = new List<string> { "eCode2" };
        var profileMapper = GetProfileMapper_ReturningProfiles(ecodes);
        var documentService = Substitute.For<IDocumentServiceClient>();
        var service = GetService(documentService, profileMapper);

        // Act
        Func<Task> act = async () =>
        {
            await service
                .GrantPermission(
                    "eCode1",
                    "driveId",
                    "directoryId",
                    PermissionLevels.Edit,
                    "correlationId");
        };

        //Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task RemovePermission_Success()
    {
        // Arrange
        var ecodes = new List<string> { "eCode2" };
        var profileMapper = GetProfileMapper_ReturningProfiles(ecodes, "correlationId");
        var documentService = Substitute.For<IDocumentServiceClient>();
        documentService
            .RemoveDirectoryPermission(Arg.Any<RemovePermissionDto>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);

        var service = GetService(documentService, profileMapper);

        // Act
        var result =  service
            .RemovePermission(
                "driveId",
                "directoryId",
                "permId",
                "correlationId");
        await result;
        
        //Assert
        result.IsCompletedSuccessfully.Should().BeTrue();
        await documentService
            .Received()
            .RemoveDirectoryPermission(Arg.Any<RemovePermissionDto>(), Arg.Any<string>());
    }

    [Fact]
    public async Task RemovePermission_PermissionNotExists_Success()
    {
        // Arrange
        var ecodes = new List<string> { "eCode2" };
        var profileMapper = GetProfileMapper_ReturningProfiles(ecodes, "correlationId");
        var documentService = Substitute.For<IDocumentServiceClient>();
        documentService
            .RemoveDirectoryPermission(Arg.Any<RemovePermissionDto>(), Arg.Any<string>())
            .Throws(new HttpRequestException("Not Found", null, HttpStatusCode.NotFound));

        var service = GetService(documentService, profileMapper);

        // Act
        var result = service
            .RemovePermission(
                "driveId",
                "directoryId",
                "permId",
                "correlationId");
        await result;

        //Assert
        result.IsCompletedSuccessfully.Should().BeTrue();
        await documentService
            .Received(1)
            .RemoveDirectoryPermission(Arg.Any<RemovePermissionDto>(), Arg.Any<string>());
    }

    [Fact]
    public async Task RemovePermission_DocumentServiceFailed_ThrowsException()
    {
        // Arrange
        var ecodes = new List<string> { "eCode2" };
        var profileMapper = GetProfileMapper_ReturningProfiles(ecodes, "correlationId");
        var documentService = Substitute.For<IDocumentServiceClient>();
        documentService
            .RemoveDirectoryPermission(Arg.Any<RemovePermissionDto>(), Arg.Any<string>())
            .Throws(new HttpRequestException("InternalServerError", null, HttpStatusCode.InternalServerError));

        var service = GetService(documentService, profileMapper);

        // Act
        Func<Task> act = async () =>
        {
            await service
                .RemovePermission(
                    "driveId",
                    "directoryId",
                    "permId",
                    "correlationId");
        };

        //Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    private ProfileMapper GetProfileMapper_ReturningProfiles(List<string> ecodes, string correlationId = "")
    {
        var repository = Substitute.For<IProfileRepository>();

        repository.GetProfiles(Arg.Is<IReadOnlyList<string>>(list => list.OrderBy(x => x).SequenceEqual(ecodes)), correlationId)
            .Returns(ecodes.ConvertAll(Fake.EmployeeDetails));

        return new ProfileMapper(repository);
    }

    private IPermissionService GetService(
        IDocumentServiceClient documentServiceClient,
        IProfileMapper profileMapper)
    {
        var logger = Substitute.For<ILogger<PermissionService>>();
        return new PermissionService(documentServiceClient, profileMapper, logger);
    }
}