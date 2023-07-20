using PEXC.Document.DataContracts.V1;

namespace PEXC.Case.Services.Workflow;

public interface IPermissionService
{
    Task<DirectoryPermissionDto?> GrantPermission(string employeeCode, string driveId, string directoryId, string permissionLevel, string correlationId);
    Task RemovePermission(string driveId, string directoryId, string permissionId, string correlationId);
}