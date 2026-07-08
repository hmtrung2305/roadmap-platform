using Microsoft.AspNetCore.Authorization;

namespace RoadmapPlatform.Infrastructure.Security;

/// <summary>
/// Represents an authorization requirement for a single permission.
/// </summary>
/// <remarks>
/// This requirement is added to an authorization policy and later evaluated by
/// a permission authorization handler.
/// </remarks>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Gets the permission code required to satisfy this authorization requirement.
    /// </summary>
    public string Permission { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionRequirement"/> class.
    /// </summary>
    /// <param name="permission">The permission code required by the policy.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the permission code is null, empty, or whitespace.
    /// </exception>
    public PermissionRequirement(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            throw new ArgumentException("Permission name cannot be empty.", nameof(permission));
        }

        Permission = permission.Trim();
    }
}
