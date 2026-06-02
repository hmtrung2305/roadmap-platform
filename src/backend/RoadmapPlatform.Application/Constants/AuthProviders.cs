namespace RoadmapPlatform.Application.Constants
{
    public static class AuthProviders
    {
        public const string Local = "local";
        public const string Google = "google";
        public const string GitHub = "github";

        public static bool IsSupported(string provider)
        {
            return provider == Local ||
                   provider == Google ||
                   provider == GitHub;
        }

        public static bool IsExternal(string provider)
        {
            return provider == Google ||
                   provider == GitHub;
        }

        public static string Normalize(string provider)
        {
            return provider.Trim().ToLowerInvariant();
        }
    }
}
