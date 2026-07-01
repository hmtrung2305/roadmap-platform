using System.ComponentModel.DataAnnotations;

namespace RoadmapPlatform.Application.DTOs.AiMentor;

public sealed class AiMentorChatRequestDto
{
    public Guid? ConversationId { get; set; }

    public string PageContext { get; set; } = "roadmap_selection";

    [Required]
    [MinLength(1)]
    [MaxLength(4000)]
    public string Message { get; set; } = string.Empty;
}