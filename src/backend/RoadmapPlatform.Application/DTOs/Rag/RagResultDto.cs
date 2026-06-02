namespace RoadmapPlatform.Application.DTOs.Rag
{
    public class RagResultDto
    {
        public string Answer { get; set; } = string.Empty;

        public string Context { get; set; } = string.Empty;

        public float Score { get; set; }
    }
}