using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class AiMentorMessage
{
    public Guid AiMentorMessageId { get; set; }

    public Guid AiMentorConversationId { get; set; }

    public string Role { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string Sources { get; set; } = null!;

    public string? AiModel { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual AiMentorConversation AiMentorConversation { get; set; } = null!;
}
