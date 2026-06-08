using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RoadmapPlatform.Application.Interfaces;
using RoadmapPlatform.Application.Interfaces.Auth;
using RoadmapPlatform.Infrastructure.Configurations;
using RoadmapPlatform.Infrastructure.Services;
using RoadmapPlatform.Infrastructure.Services.Email;

namespace RoadmapPlatform.Api.Extensions;

public static class EmailServiceCollectionExtensions
{
    public static IServiceCollection AddEmailServices(
            this IServiceCollection services,
            IConfiguration configuration)
    {

        services.Configure<EmailVerificationSettings>(
            configuration.GetSection("EmailVerification"));

        services.Configure<SmtpEmailSettings>(
            configuration.GetSection("SmtpEmail"));

        services.Configure<BrevoEmailSettings>(
            configuration.GetSection("BrevoEmail"));


        services.AddScoped<IEmailVerificationService, EmailVerificationService>();

        var smtpEnabled = configuration.GetValue<bool>("SmtpEmail:Enabled");
        var brevoEnabled = configuration.GetValue<bool>("BrevoEmail:Enabled");

        services.AddScoped<ConsoleEmailSender>();
        services.AddScoped<SmtpEmailSender>();
        services.AddScoped<BrevoEmailSender>();

        services.AddScoped<IEmailSender>(sp =>
        {
            if (smtpEnabled)
            {
                return sp.GetRequiredService<SmtpEmailSender>();
            }

            if (brevoEnabled)
            {
                return sp.GetRequiredService<BrevoEmailSender>();
            }

            return sp.GetRequiredService<ConsoleEmailSender>();
        });

        return services;
    }
}
