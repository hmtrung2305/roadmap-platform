using RoadmapPlatform.Application.DTOs.AiMentor;
using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.Interfaces.AiMentor
{
    public interface IAiMentorService
    {
        Task<IReadOnlyList<AiMentorConversationDto>> GetConversationsAsync(Guid id, CancellationToken cancellationToken);

        Task<IReadOnlyList<AiMentorMessageDto>> GetMessagesAsync(Guid id, Guid conversationId, CancellationToken cancellationToken);

        Task<AiMentorChatResponseDto> AskAsync(Guid id, AiMentorChatRequestDto request, CancellationToken cancellationToken);

        Task ArchiveConversationAsync(Guid id, Guid conversationId, CancellationToken cancellationToken);
    }
}
