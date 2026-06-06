using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RoadmapPlatform.Application.Interfaces.Security;
using RoadmapPlatform.Infrastructure.Configurations;

namespace RoadmapPlatform.Infrastructure.Services.Security;

public sealed class TurnstileCaptchaService : ICaptchaService
{
    private const string TurnstileProvider = "Turnstile";

    private readonly HttpClient _httpClient;
    private readonly CaptchaSettings _settings;
    private readonly ILogger<TurnstileCaptchaService> _logger;

    public TurnstileCaptchaService(
        HttpClient httpClient,
        IOptions<CaptchaSettings> settings,
        ILogger<TurnstileCaptchaService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<CaptchaVerificationResult> VerifyAsync(
        string? token,
        string? expectedAction,
        string? remoteIp,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            return CaptchaVerificationResult.Valid();
        }

        if (!string.Equals(_settings.Provider, TurnstileProvider, StringComparison.OrdinalIgnoreCase))
        {
            return CaptchaVerificationResult.Invalid("Unsupported CAPTCHA provider.");
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return CaptchaVerificationResult.Invalid("Please complete the CAPTCHA challenge.");
        }

        if (string.IsNullOrWhiteSpace(_settings.SecretKey))
        {
            throw new InvalidOperationException("CAPTCHA secret key is not configured.");
        }

        var form = new Dictionary<string, string>
        {
            ["secret"] = _settings.SecretKey,
            ["response"] = token
        };

        if (!string.IsNullOrWhiteSpace(remoteIp))
        {
            form["remoteip"] = remoteIp;
        }

        using var content = new FormUrlEncodedContent(form);
        using var response = await _httpClient.PostAsync(
            _settings.VerifyUrl,
            content,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "CAPTCHA verification failed with status code {StatusCode}.",
                response.StatusCode);

            return CaptchaVerificationResult.Invalid("CAPTCHA verification failed. Please try again.");
        }

        var verification = await response.Content.ReadFromJsonAsync<TurnstileVerificationResponse>(
            cancellationToken: cancellationToken);

        if (verification?.Success != true)
        {
            return CaptchaVerificationResult.Invalid(
                "CAPTCHA verification failed. Please try again.",
                verification?.ErrorCodes);
        }

        if (!string.IsNullOrWhiteSpace(expectedAction) &&
            !string.Equals(verification.Action, expectedAction, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "CAPTCHA action mismatch. Expected {ExpectedAction}, received {ActualAction}.",
                expectedAction,
                verification.Action);

            return CaptchaVerificationResult.Invalid("CAPTCHA verification failed. Please try again.");
        }

        return CaptchaVerificationResult.Valid();
    }

    private sealed class TurnstileVerificationResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("error-codes")]
        public string[] ErrorCodes { get; set; } = [];

        [JsonPropertyName("action")]
        public string? Action { get; set; }
    }
}