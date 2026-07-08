namespace RoadmapPlatform.Infrastructure.Configurations
{
    /// <summary>
    /// Represents Google OAuth configuration values loaded from application settings.
    /// </summary>
    public class GoogleAuthSettings
    {
        /// <summary>
        /// Gets or sets the Google OAuth client identifier.
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Google OAuth client secret.
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;
    }
}
