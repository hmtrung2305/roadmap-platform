namespace RoadmapPlatform.Application.DTOs.AiMentor;

public sealed class AiMentorMessageDto
{
    public Guid AiMentorMessageId { get; set; }

    public Guid AiMentorConversationId { get; set; }

    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public IReadOnlyList<AiMentorSourceDto> Sources { get; set; } = [];

    public string? AiModel { get; set; }

    public DateTime CreatedAt { get; set; }
}