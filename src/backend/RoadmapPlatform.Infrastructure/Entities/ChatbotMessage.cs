using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class ChatbotMessage
{
    public Guid RequestId { get; set; }

    public Guid ConversationId { get; set; }

    public string ContentMessage { get; set; } = null!;

    public string? Metadata { get; set; }

    public virtual Conversation Conversation { get; set; } = null!;
}
