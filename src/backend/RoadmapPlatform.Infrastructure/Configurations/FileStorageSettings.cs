namespace RoadmapPlatform.Infrastructure.Configurations;

public sealed class FileStorageSettings
{
    public const string SectionName = "FileStorage";

    public string Provider { get; set; } = "Local";

    public string LocalFolder { get; set; } = "storage";

    public SupabaseFileStorageSettings Supabase { get; set; } = new();
}

public sealed class SupabaseFileStorageSettings
{
    public string Url { get; set; } = string.Empty;

    public string ServiceRoleKey { get; set; } = string.Empty;

    public string Bucket { get; set; } = string.Empty;

    public bool UsePublicUrls { get; set; }
}
