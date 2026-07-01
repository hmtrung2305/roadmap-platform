namespace RoadmapPlatform.Application.DTOs.AiMentor;

public sealed class AiMentorSourceDto
{
    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Detail { get; set; }

    public string? Url { get; set; }
}