namespace RoadmapPlatform.Application.Constants;

public static class PermissionPolicyNames
{
    public const string Prefix = "Permission:";
    public const string AnyPrefix = "PermissionAny:";
    private const char Separator = '|';

    public static string For(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            throw new ArgumentException("Permission name cannot be empty.", nameof(permission));
        }

        return $"{Prefix}{permission}";
    }

    public static string ForAny(params string[] permissions)
    {
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
