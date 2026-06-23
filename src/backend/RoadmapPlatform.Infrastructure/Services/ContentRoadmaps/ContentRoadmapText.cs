using Microsoft.EntityFrameworkCore;

namespace RoadmapPlatform.Infrastructure.Services.ContentRoadmaps;

internal static class ContentRoadmapText
{
    public static string NormalizeRequiredText(string? value, string errorMessage)
    {
        var normalized = NormalizeOptionalText(value);
        if (normalized == null)
        {
            throw new ArgumentException(errorMessage);
        }

        return normalized;
    }

    public static string? NormalizeOptionalText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    public static string BuildContainsPattern(string value)
    {
        var escaped = value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

        return $"%{escaped}%";
    }
}
