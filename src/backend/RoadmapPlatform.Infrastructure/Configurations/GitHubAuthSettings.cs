namespace RoadmapPlatform.Infrastructure.Configurations
{
    /// <summary>
    /// Represents GitHub OAuth configuration values loaded from application settings.
    /// </summary>
    public class GitHubAuthSettings
    {
        /// <summary>
        /// Gets or sets the GitHub OAuth client identifier.
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the GitHub OAuth client secret.
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;
    }
}
