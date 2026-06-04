using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace RoadmapPlatform.Infrastructure.Security;

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionCache _permissionCache;

    public PermissionHandler(IPermissionCache permissionCache)
    {
        _permissionCache = permissionCache;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, 
        PermissionRequirement requirement)
    {
        var roles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        if (!roles.Any())
        {
            return;
        }

        var permissionsMap = await _permissionCache.GetPermissionsMapAsync();

        foreach (var role in roles)
        {
            if (permissionsMap.TryGetValue(role, out var permissions) && 
                permissions.Contains(requirement.Permission))
            {
                context.Succeed(requirement);
                return;
            }
        }
    }
}
