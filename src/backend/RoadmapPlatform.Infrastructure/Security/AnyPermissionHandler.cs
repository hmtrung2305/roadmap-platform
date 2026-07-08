using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace RoadmapPlatform.Infrastructure.Security;

/// <summary>
/// Handles authorization requirements that can be satisfied by any one of multiple permissions.
/// </summary>
/// <remarks>
/// The handler reads role claims from the current user, resolves the permissions
/// assigned to those roles from the permission cache, and succeeds the requirement
/// when at least one role contains at least one accepted permission.
/// </remarks>
public sealed class AnyPermissionHandler : AuthorizationHandler<AnyPermissionRequirement>
{
    private readonly IPermissionCache _permissionCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnyPermissionHandler"/> class.
    /// </summary>
    /// <param name="permissionCache">The permission cache used to resolve role permissions.</param>
    public AnyPermissionHandler(IPermissionCache permissionCache)
    {
        _permissionCache = permissionCache;
    }

    /// <summary>
    /// Evaluates whether the current authenticated user has at least one accepted permission.
    /// </summary>
    /// <param name="context">The current authorization context.</param>
    /// <param name="requirement">The any-permission requirement to evaluate.</param>
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
