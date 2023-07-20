namespace PEXC.Case.Domain;

public record Permission(
    string PermissionId, 
    string EmployeeEcode, 
    PermissionScope PermissionScope, 
    PermissionType PermissionType, 
    DateTime ValidFrom, 
    bool IsActive);