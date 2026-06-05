namespace RoadmapPlatform.Infrastructure.Configurations
{
    public sealed class FileStorageSettings
    {
        public string Provider { get; set; } = "Local";

        public string LocalFolder { get; set; } = "docs";
    }

    public sealed class SupabaseStorageSettings
    {
        public string Url { get; set; } = string.Empty;

        public string ServiceRoleKey { get; set; } = string.Empty;

        public string Bucket { get; set; } = "roadmap-docs";
    }
}
