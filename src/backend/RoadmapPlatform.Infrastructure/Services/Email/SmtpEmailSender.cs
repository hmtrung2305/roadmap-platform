using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using RoadmapPlatform.Application.Interfaces;
using RoadmapPlatform.Infrastructure.Configurations;

namespace RoadmapPlatform.Infrastructure.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpEmailSettings _settings;

        public SmtpEmailSender(IOptions<SmtpEmailSettings> options)
        {
            _settings = options.Value;
        }

        public async Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken)
        {
            if (!_settings.Enabled)
            {
                throw new InvalidOperationException("SMTP email sender is not enabled");
            }

            if (string.IsNullOrWhiteSpace(_settings.Host))
            {
                throw new InvalidOperationException("SMTP host was not configured");
            }

            if (string.IsNullOrWhiteSpace(_settings.Username))
            {
                throw new InvalidOperationException("SMTP username was not configured");
            }

            if (string.IsNullOrWhiteSpace(_settings.Password))
            {
                throw new InvalidOperationException("SMTP password was not configured");
            }

            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            message.Body = new TextPart("plain")
            {
                Text = body
            };

            using var client = new SmtpClient();

            var secureSocketOptions = _settings.UseStartTls
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.Auto;

            await client.ConnectAsync(_settings.Host, _settings.Port, secureSocketOptions);

            await client.AuthenticateAsync(_settings.Username, _settings.Password);

            await client.SendAsync(message);

            await client.DisconnectAsync(true);
        }
    }
}