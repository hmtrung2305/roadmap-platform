namespace RoadmapPlatform.Application.DTOs.Chat
{
    public class ChatRequestDto
    {
        public Guid ResourceId { get; set; }

        public string Prompt { get; set; } = string.Empty;
    }
}