using Microsoft.AspNetCore.Authorization;

namespace RoadmapPlatform.Infrastructure.Security;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            throw new ArgumentException("Permission name cannot be empty.", nameof(permission));
        }

        Permission = permission.Trim();
    }
}
