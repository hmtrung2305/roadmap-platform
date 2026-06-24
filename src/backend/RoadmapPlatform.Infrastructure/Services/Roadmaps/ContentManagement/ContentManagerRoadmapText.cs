using Microsoft.EntityFrameworkCore;
using System.Text;

namespace RoadmapPlatform.Infrastructure.Services.Roadmaps.ContentManagement;

internal static class ContentManagerRoadmapText
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


    public static string Slugify(string value)
    {
        var normalized = NormalizeRequiredText(value, "Slug value is required.").ToLowerInvariant();
        var builder = new StringBuilder(normalized.Length);
        var previousDash = false;

        foreach (var character in normalized)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousDash = false;
            }
            else if (!previousDash)
            {
                builder.Append('-');
                previousDash = true;
            }
        }

        var slug = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "node" : slug;
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
