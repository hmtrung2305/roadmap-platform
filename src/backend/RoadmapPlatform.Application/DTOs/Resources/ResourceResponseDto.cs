namespace RoadmapPlatform.Application.DTOs.Resources
{
    public class ResourceResponseDto
    {
        public Guid ResourceId { get; set; }

        public Guid SkillId { get; set; }

        public string? Title { get; set; }

        public string Url { get; set; } = string.Empty;

        public DateTime? CreatedAt { get; set; }

        public string? Metadata { get; set; }

        public string? SkillName { get; set; }
    }
}