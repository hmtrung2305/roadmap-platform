namespace RoadmapPlatform.Application.Interfaces.Security;

public interface ICaptchaProtectedRequest
{
    string? CaptchaToken { get; set; }
}

public interface ICaptchaService
{
    Task<CaptchaVerificationResult> VerifyAsync(
        string? token,
        string? expectedAction,
        string? remoteIp,
        CancellationToken cancellationToken = default);
}

public sealed record CaptchaVerificationResult(
    bool Success,
    string? Message = null,
    IReadOnlyCollection<string>? ErrorCodes = null)
{
    public static CaptchaVerificationResult Valid() => new(true);

    public static CaptchaVerificationResult Invalid(
        string message,
        IReadOnlyCollection<string>? errorCodes = null) =>
        new(false, message, errorCodes);
}