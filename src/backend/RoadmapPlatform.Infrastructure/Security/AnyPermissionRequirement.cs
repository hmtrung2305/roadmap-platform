using Microsoft.AspNetCore.Authorization;

namespace RoadmapPlatform.Infrastructure.Security;

public sealed class AnyPermissionRequirement : IAuthorizationRequirement
{
    public IReadOnlySet<string> Permissions { get; }

    public AnyPermissionRequirement(IEnumerable<string> permissions)
    {
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
