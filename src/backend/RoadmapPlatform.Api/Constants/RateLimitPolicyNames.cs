namespace RoadmapPlatform.Api.Constants;

/// <summary>
/// Defines the names of rate-limit policies applied by API controllers.
/// </summary>
public static class RateLimitPolicyNames
{
    /// <summary>
    /// Limits authentication operations that are sensitive to brute-force traffic.
    /// </summary>
    public const string AuthStrict = "auth-strict";

    /// <summary>
    /// Limits computationally expensive AI operations.
    /// </summary>
    public const string AiExpensive = "ai-expensive";

    /// <summary>
    /// Limits resource-intensive upload operations.
    /// </summary>
    public const string UploadExpensive = "upload-expensive";

    /// <summary>
    /// Limits administrative write operations.
    /// </summary>
    public const string AdminMutation = "admin-mutation";

    /// <summary>
    /// Limits calls that depend on an external API.
    /// </summary>
    public const string ExternalApi = "external-api";
}
