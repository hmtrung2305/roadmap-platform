using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class Conversation
{
    public Guid ConversationId { get; set; }

    public Guid UserId { get; set; }

    public Guid ResourceId { get; set; }

    public string? Title { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<ChatbotMessage> ChatbotMessages { get; set; } = new List<ChatbotMessage>();

    public virtual Resource Resource { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
