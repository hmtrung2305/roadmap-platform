using RoadmapPlatform.Application.DTOs.Chat;

namespace RoadmapPlatform.Application.Interfaces.Chat
{
    public interface IChatService
    {
        Task<ChatResponseDto> ChatAsync(ChatRequestDto request);
    }
}