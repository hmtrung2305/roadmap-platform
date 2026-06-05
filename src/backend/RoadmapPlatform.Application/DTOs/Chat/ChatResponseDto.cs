using RoadmapPlatform.Application.DTOs.AiCredits;

namespace RoadmapPlatform.Application.DTOs.Chat
{
    public class ChatResponseDto
    {
        public string Response { get; set; } = string.Empty;

        public string? DebugContextUsed { get; set; }

        public float DebugScore { get; set; }

        public AiCreditStatusDto? Credits { get; set; }
    }
}