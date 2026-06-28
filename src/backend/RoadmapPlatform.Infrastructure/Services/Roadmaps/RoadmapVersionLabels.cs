using System.Text.RegularExpressions;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.Roadmaps;

internal static class RoadmapVersionLabels
{
    private static readonly Regex LegacyVersionSuffixPattern = new(@"\s+v\d+(?:\.\d+\.\d+)?\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string Format(RoadmapVersion version)
    {
        return $"v{version.MajorVersion}.{version.MinorVersion}.{version.PatchVersion}";
    }

    public static IOrderedEnumerable<RoadmapVersion> OrderNewestFirst(IEnumerable<RoadmapVersion> versions)
    {
        return versions
            .OrderByDescending(version => version.MajorVersion)
            .ThenByDescending(version => version.MinorVersion)
            .ThenByDescending(version => version.PatchVersion)
            .ThenByDescending(version => version.VersionNumber);
    }

    public static string RemoveLegacyVersionSuffix(string title)
    {
        return LegacyVersionSuffixPattern.Replace(title, string.Empty).Trim();
    }
}
