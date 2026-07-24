using Microsoft.AspNetCore.Authorization;

namespace DocFlow.Application.Common.Authorization;

public class HasPermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Permission:";

    public HasPermissionAttribute(string permissionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionName);
        Policy = $"{PolicyPrefix}{permissionName}";
    }
}
