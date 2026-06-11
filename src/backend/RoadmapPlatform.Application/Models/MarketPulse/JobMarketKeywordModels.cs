namespace RoadmapPlatform.Application.Models.MarketPulse;

public sealed record JobMarketKeywordDefinition(
    string Name,
    string Slug,
    IReadOnlyList<string> Aliases);

public sealed record JobMarketKeywordFrequency(
    string SkillName,
    string SkillSlug,
    int MentionCount,
    int PostingCount);