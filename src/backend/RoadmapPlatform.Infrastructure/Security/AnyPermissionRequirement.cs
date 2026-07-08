using Microsoft.AspNetCore.Authorization;

namespace RoadmapPlatform.Infrastructure.Security;

/// <summary>
/// Represents an authorization requirement that can be satisfied by any one of multiple permissions.
/// </summary>
/// <remarks>
/// This requirement is used for endpoints that can be accessed through more than one permission.
/// The corresponding authorization handler succeeds when the current user has at least one
/// permission from the provided permission set.
/// </remarks>
public sealed class AnyPermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Gets the set of accepted permission codes.
    /// </summary>
    public IReadOnlySet<string> Permissions { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AnyPermissionRequirement"/> class.
    /// </summary>
    /// <param name="permissions">
    /// The permission codes accepted by this requirement. At least one valid permission is required.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when no valid permission code is provided.
    /// </exception>
    public AnyPermissionRequirement(IEnumerable<string> permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        var values = permissions
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Select(permission => permission.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        if (values.Count == 0)
        {
            throw new ArgumentException("At least one permission is required.", nameof(permissions));
        }

        Permissions = values;
    }
}
