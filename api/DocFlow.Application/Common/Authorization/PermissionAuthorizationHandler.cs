using System.Security.Claims;
using DocFlow.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace DocFlow.Application.Common.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionService _permissionService;

    public PermissionAuthorizationHandler(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            return;

        var hasPermission = await _permissionService.UserHasPermissionAsync(
            userId, requirement.PermissionName, CancellationToken.None);

        if (hasPermission)
            context.Succeed(requirement);
    }
}
