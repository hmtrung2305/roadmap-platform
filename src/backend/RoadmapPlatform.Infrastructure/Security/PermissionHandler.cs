using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace RoadmapPlatform.Infrastructure.Security;

/// <summary>
/// Handles authorization requirements that require a single permission.
/// </summary>
/// <remarks>
/// The handler reads role claims from the current user, resolves the permissions
/// assigned to those roles from the permission cache, and succeeds the requirement
/// when at least one role contains the required permission.
/// </remarks>
public sealed class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionCache _permissionCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionHandler"/> class.
    /// </summary>
    /// <param name="permissionCache">The permission cache used to resolve role permissions.</param>
    public PermissionHandler(IPermissionCache permissionCache)
    {
        _permissionCache = permissionCache;
    }

    /// <summary>
    /// Evaluates whether the current authenticated user satisfies the required permission.
    /// </summary>
    /// <param name="context">The current authorization context.</param>
    /// <param name="requirement">The permission requirement to evaluate.</param>
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
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
            if (permissionsMap.TryGetValue(roleName, out var permissions) &&
                permissions.Contains(requirement.Permission))
            {
                context.Succeed(requirement);
                return;
            }
        }
    }
}
