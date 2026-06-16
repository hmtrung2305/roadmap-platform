namespace RoadmapPlatform.Application.Exceptions;

public sealed class GitHubIntegrationException : Exception
{
    public GitHubIntegrationException(
        string code,
        string message,
        int statusCode,
        int? retryAfterSeconds = null)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
        RetryAfterSeconds = retryAfterSeconds;
    }

    public string Code { get; }

    public int StatusCode { get; }

    public int? RetryAfterSeconds { get; }

    public static GitHubIntegrationException NotLinked()
    {
        return new GitHubIntegrationException(
            "GITHUB_NOT_LINKED",
            "GitHub is not connected. Please connect GitHub before syncing repositories.",
            409);
    }

    public static GitHubIntegrationException TokenMissing()
    {
        return new GitHubIntegrationException(
            "GITHUB_TOKEN_MISSING",
            "GitHub connection is incomplete. Please reconnect GitHub.",
            409);
    }

    public static GitHubIntegrationException TokenInvalid()
    {
        return new GitHubIntegrationException(
            "GITHUB_TOKEN_INVALID",
            "GitHub connection has expired or was revoked. Please reconnect GitHub.",
            409);
    }

    public static GitHubIntegrationException RateLimited(int? retryAfterSeconds = null)
    {
        return new GitHubIntegrationException(
            "GITHUB_RATE_LIMITED",
            "GitHub rate limit was reached. Please try again later.",
            429,
            retryAfterSeconds);
    }

    public static GitHubIntegrationException ApiFailure(string? message = null)
    {
        return new GitHubIntegrationException(
            "GITHUB_API_ERROR",
            string.IsNullOrWhiteSpace(message)
                ? "GitHub could not be reached right now. Please try again later."
                : message,
            502);
    }
}
