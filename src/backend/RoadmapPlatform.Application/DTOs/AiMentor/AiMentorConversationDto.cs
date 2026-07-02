using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.AiMentor
{
    public sealed class AiMentorConversationDto
    {
        public Guid AiMentorConversationId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string PageContext { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
