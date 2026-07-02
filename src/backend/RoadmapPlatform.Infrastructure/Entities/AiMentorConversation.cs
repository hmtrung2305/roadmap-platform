using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class AiMentorConversation
{
    public Guid AiMentorConversationId { get; set; }

    public Guid UserId { get; set; }

    public string Title { get; set; } = null!;

    public string PageContext { get; set; } = null!;

    public DateTime? ArchivedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<AiMentorMessage> AiMentorMessages { get; set; } = new List<AiMentorMessage>();

    public virtual User User { get; set; } = null!;
}
