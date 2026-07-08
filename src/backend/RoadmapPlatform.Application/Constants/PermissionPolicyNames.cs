namespace RoadmapPlatform.Application.Constants;

/// <summary>
/// Defines naming conventions for permission-based authorization policies.
/// </summary>
/// <remarks>
/// These policy names are used by custom authorization attributes and parsed by
/// the permission policy provider to dynamically create authorization policies.
/// </remarks>
public static class PermissionPolicyNames
{
    /// <summary>
    /// The policy prefix used for policies that require one specific permission.
    /// </summary>
    public const string Prefix = "Permission:";

    /// <summary>
    /// The policy prefix used for policies that can be satisfied by any one of multiple permissions.
    /// </summary>
    public const string AnyPrefix = "PermissionAny:";

    private const char Separator = '|';

    /// <summary>
    /// Creates a policy name for a single required permission.
    /// </summary>
    /// <param name="permission">The required permission code.</param>
    /// <returns>The generated authorization policy name.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the permission code is null, empty, or whitespace.
    /// </exception>
    public static string For(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            throw new ArgumentException("Permission name cannot be empty.", nameof(permission));
        }

        var value = permission.Trim();
        if (value.Contains(Separator))
        {
            throw new ArgumentException($"Permission name cannot contain '{Separator}'.", nameof(permission));
        }

        return $"{Prefix}{permission}";
    }

    /// <summary>
    /// Creates a policy name for a requirement that accepts any one of multiple permissions.
    /// </summary>
    /// <param name="permissions">The accepted permission codes.</param>
    /// <returns>The generated authorization policy name.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when no permission is provided, a permission is empty, or a permission contains the separator character.
    /// </exception>
    public static string ForAny(params string[] permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        if (permissions.Length == 0)
        {
            throw new ArgumentException("At least one permission is required.", nameof(permissions));
        }

        var normalizedPermissions = permissions
            .Select(permission =>
            {
                if (string.IsNullOrWhiteSpace(permission))
                {
                    throw new ArgumentException("Permission name cannot be empty.", nameof(permissions));
                }

                var value = permission.Trim();
                if (value.Contains(Separator))
                {
                    throw new ArgumentException($"Permission name cannot contain '{Separator}'.", nameof(permissions));
                }

                return value;
            })
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return $"{AnyPrefix}{string.Join(Separator, normalizedPermissions)}";
    }

    /// <summary>
    /// Attempts to extract a single permission code from a policy name.
    /// </summary>
    /// <param name="policyName">The policy name to parse.</param>
    /// <param name="permission">The extracted permission code when parsing succeeds.</param>
    /// <returns>True when the policy name matches the single-permission convention; otherwise, false.</returns>
    public static bool TryGetPermission(string policyName, out string permission)
    {
        permission = string.Empty;

        if (string.IsNullOrWhiteSpace(policyName) ||
            !policyName.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var value = policyName[Prefix.Length..].Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        permission = value;
        return true;
    }

    /// <summary>
    /// Attempts to extract multiple accepted permission codes from a policy name.
    /// </summary>
    /// <param name="policyName">The policy name to parse.</param>
    /// <param name="permissions">The extracted permission codes when parsing succeeds.</param>
    /// <returns>True when the policy name matches the any-permission convention; otherwise, false.</returns>
    public static bool TryGetAnyPermissions(string policyName, out IReadOnlyList<string> permissions)
    {
        permissions = Array.Empty<string>();

        if (string.IsNullOrWhiteSpace(policyName) ||
            !policyName.StartsWith(AnyPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var values = policyName[AnyPrefix.Length..]
            .Split(Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (values.Length == 0)
        {
            return false;
        }

        permissions = values;
        return true;
    }
}
