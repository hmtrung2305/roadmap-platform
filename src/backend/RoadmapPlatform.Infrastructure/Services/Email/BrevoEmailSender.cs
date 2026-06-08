using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RoadmapPlatform.Application.Interfaces;
using RoadmapPlatform.Infrastructure.Configurations;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace RoadmapPlatform.Infrastructure.Services.Email;

public sealed class BrevoEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly BrevoEmailSettings _settings;
    private readonly ILogger<BrevoEmailSender> _logger;

    public BrevoEmailSender(
        HttpClient httpClient,
        IOptions<BrevoEmailSettings> options,
        ILogger<BrevoEmailSender> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new InvalidOperationException("Brevo API key is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_settings.FromEmail))
        {
            throw new InvalidOperationException("Brevo sender email is not configured.");
        }

        var payload = new BrevoSendEmailRequest
        {
            Sender = new BrevoEmailContact
            {
                Name = _settings.FromName,
                Email = _settings.FromEmail
            },
            To =
            [
                new BrevoEmailContact
                {
                    Email = toEmail
                }
            ],
            Subject = subject,
            HtmlContent = htmlBody
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.brevo.com/v3/smtp/email");

        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        request.Headers.Add("api-key", _settings.ApiKey);

        request.Content = JsonContent.Create(payload);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogError(
                "Brevo email send failed. StatusCode: {StatusCode}. Body: {Body}",
                response.StatusCode,
                errorBody);

            throw new InvalidOperationException("Failed to send email through Brevo.");
        }
    }

    private sealed class BrevoSendEmailRequest
    {
        public required BrevoEmailContact Sender { get; init; }
        public required List<BrevoEmailContact> To { get; init; }
        public required string Subject { get; init; }
        public required string HtmlContent { get; init; }
    }

    private sealed class BrevoEmailContact
    {
        public string? Name { get; init; }
        public required string Email { get; init; }
    }
}