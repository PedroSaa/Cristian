using Microsoft.AspNetCore.Authorization;

namespace DocFlow.Application.Common.Authorization;

public class PermissionRequirement : IAuthorizationRequirement
{
    public string PermissionName { get; }

    public PermissionRequirement(string permissionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionName);
        PermissionName = permissionName;
    }
}
