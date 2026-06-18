namespace RoadmapPlatform.Application.Constants;

public static class PermissionPolicyNames
{
    public const string Prefix = "Permission:";

    public static string For(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            throw new ArgumentException("Permission name cannot be empty.", nameof(permission));
        }

        return $"{Prefix}{permission}";
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
}
