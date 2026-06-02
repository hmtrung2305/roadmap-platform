namespace RoadmapPlatform.Application.DTOs.Resources
{
    public class CreateResourceRequestDto
    {
        public string Title { get; set; } = string.Empty;

        public string? Type { get; set; }

        public string? SourceUrl { get; set; }
    }
}