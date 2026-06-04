namespace RoadmapPlatform.Infrastructure.Configurations
{
    public class AiSettings
    {
        public string ApiKey { get; set; } = string.Empty;

        public string EmbeddingModel { get; set; } = "gemini-embedding-2";

        public string GenerationModel { get; set; } = "gemini-2.5-flash";
    }
}