namespace RoadmapPlatform.Application.DTOs.AiMentor;

public sealed class AiMentorChatResponseDto
{
    public AiMentorConversationDto Conversation { get; set; } = null!;

    public AiMentorMessageDto UserMessage { get; set; } = null!;

    public AiMentorMessageDto AssistantMessage { get; set; } = null!;
}