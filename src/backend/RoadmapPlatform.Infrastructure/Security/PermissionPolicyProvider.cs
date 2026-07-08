using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using RoadmapPlatform.Application.Constants;

namespace RoadmapPlatform.Infrastructure.Security;

/// <summary>
/// Provides authorization policies for permission-based authorization.
/// </summary>
/// <remarks>
/// This provider first checks the default authorization policy collection.
/// If no static policy is found, it dynamically creates permission policies
/// based on the permission policy name convention.
/// </remarks>
public sealed class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionPolicyProvider"/> class.
    /// </summary>
    /// <param name="options">The authorization options configured for the application.</param>
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
    {
    }

    /// <summary>
    /// Gets an authorization policy by name.
    /// </summary>
    /// <param name="policyName">The policy name requested by the authorization system.</param>
    /// <returns>
    /// A statically configured policy, a dynamically generated permission policy,
    /// or null when the policy name is not recognized.
    /// </returns>
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var policy = await base.GetPolicyAsync(policyName);
        if (policy is not null)
        {
            return policy;
        }

        if (PermissionPolicyNames.TryGetPermission(policyName, out var permission))
        {
            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();
        }

        if (PermissionPolicyNames.TryGetAnyPermissions(policyName, out var permissions))
        {
            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new AnyPermissionRequirement(permissions))
                .Build();
        }

        return null;
    }
}
