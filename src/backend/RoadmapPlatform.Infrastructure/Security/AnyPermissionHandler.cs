using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace RoadmapPlatform.Infrastructure.Security;

public sealed class AnyPermissionHandler : AuthorizationHandler<AnyPermissionRequirement>
{
    private readonly IPermissionCache _permissionCache;

    public AnyPermissionHandler(IPermissionCache permissionCache)
    {
        _permissionCache = permissionCache;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AnyPermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var roleNames = context.User
            .FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim().ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (roleNames.Count == 0)
        {
            return;
        }

        var permissionsMap = await _permissionCache.GetPermissionsMapAsync();

        foreach (var roleName in roleNames)
        {
            if (!permissionsMap.TryGetValue(roleName, out var permissions))
            {
                continue;
            }

            if (requirement.Permissions.Any(permissions.Contains))
            {
                context.Succeed(requirement);
                return;
            }
        }
    }
}
