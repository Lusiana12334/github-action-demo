using System.Net;
using Microsoft.Extensions.Logging;
using PEXC.Document.Client;
using PEXC.Document.DataContracts.V1;

namespace PEXC.Case.Services.Workflow;

internal class PermissionService : IPermissionService
{
    private readonly IDocumentServiceClient _documentService;
    private readonly IProfileMapper _profileMapper;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
        IDocumentServiceClient documentService,
        IProfileMapper profileMapper,
        ILogger<PermissionService> logger)
    {
        _documentService = documentService;
        _profileMapper = profileMapper;
        _logger = logger;
    }

    public async Task<DirectoryPermissionDto?> GrantPermission(
        string employeeCode,
        string driveId,
        string directoryId,
        string permissionLevel,
        string correlationId)
    {
        var profiles = await _profileMapper.GetEmployeeProfiles(new[] { employeeCode }, correlationId);

        if (!profiles.TryGetValue(employeeCode, out var userProfile))
        {
            throw new Exception($"Cannot grant permission for employeeCode: [{employeeCode}], because the employee profile was not found.");
        }

        var permission = await _documentService
            .GrantDirectoryPermission(
                new GrantPermissionDto(
                    driveId,
                    directoryId,
                    permissionLevel,
                    new[] { userProfile.Email! }),
                correlationId);

        return permission
            .FirstOrDefault(p =>
                string.Equals(p.GrantedToEmail, userProfile.Email!, StringComparison.OrdinalIgnoreCase));
    }

    public async Task RemovePermission(string driveId, string directoryId, string permissionId, string correlationId)
    {
        try
        {
            var removePermissionDto = new RemovePermissionDto(driveId, directoryId, permissionId);
            await _documentService.RemoveDirectoryPermission(removePermissionDto, correlationId);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning(ex,
                "Permission does not exist in SharePoint. DriveId:{driveId}, DirectoryId:{directoryId}, PermissionId:{permissionId}",
                driveId, directoryId, permissionId);
        }
    }
}