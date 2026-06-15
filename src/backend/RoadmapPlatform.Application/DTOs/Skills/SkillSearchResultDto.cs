namespace RoadmapPlatform.Application.DTOs.Skills;

public sealed class SkillSearchResultDto
{
    public IReadOnlyList<SkillLookupDto> Items { get; set; } = [];

    public int TotalCount { get; set; }

    public int Limit { get; set; }

    public int Offset { get; set; }

    public bool HasMore => Offset + Items.Count < TotalCount;
}
