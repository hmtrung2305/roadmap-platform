using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RoadmapPlatform.Application.Interfaces;
using RoadmapPlatform.Application.Interfaces.Auth;
using RoadmapPlatform.Application.Interfaces.Chat;
using RoadmapPlatform.Application.Interfaces.GitHub;
using RoadmapPlatform.Application.Interfaces.Portfolio;
using RoadmapPlatform.Application.Interfaces.Rag;
using RoadmapPlatform.Application.Interfaces.Users;
using RoadmapPlatform.Application.Interfaces.Resources;
using RoadmapPlatform.Infrastructure.Clients;
using RoadmapPlatform.Infrastructure.Configurations;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services;
using RoadmapPlatform.Infrastructure.Services.Auth;
using RoadmapPlatform.Infrastructure.Services.Chat;
using RoadmapPlatform.Infrastructure.Services.Email;
using RoadmapPlatform.Infrastructure.Services.GitHub;
using RoadmapPlatform.Infrastructure.Services.Portfolio;
using RoadmapPlatform.Infrastructure.Services.Rag;
using RoadmapPlatform.Infrastructure.Services.Resources;
using RoadmapPlatform.Infrastructure.Services.Users;

namespace RoadmapPlatform.Infrastructure.Extensions
{
    // Class này dùng để đăng ký các service thuộc tầng Infrastructure.
    // Những phần nên đặt ở đây gồm: DbContext, database provider,
    // email sender, GitHub API client, RAG service, Python service client,
    // file storage, external API clients, và các implementation phụ thuộc hệ thống bên ngoài.
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions =>
                    {
                        npgsqlOptions.UseVector();
                    }));

            // JWT Settings
            services.Configure<JwtSettings>(
                configuration.GetSection("Jwt"));

            // Email Settings
            services.Configure<EmailVerificationSettings>(
                configuration.GetSection("EmailVerification"));

            services.Configure<SmtpEmailSettings>(
                configuration.GetSection("SmtpEmail"));

            // AI, RAG Settings
            services.Configure<AiSettings>(configuration.GetSection("Ai"));
            services.Configure<RagSettings>(configuration.GetSection("Rag"));

            // Đăng ký implementation cho external services ở đây sau.
            // Ví dụ:

            // Authentication Services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IOAuthLoginService, OAuthLoginService>();
            services.AddScoped<IAuthProviderService, AuthProviderService>();
            services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

            // Email Services
            services.AddScoped<IEmailVerificationService, EmailVerificationService>();
            var smtpEnabled = configuration.GetValue<bool>("SmtpEmail:Enabled");

            if (smtpEnabled)
            {
                services.AddScoped<IEmailSender, SmtpEmailSender>();
            }
            else
            {
                services.AddScoped<IEmailSender, ConsoleEmailSender>();
            }

            // User Services
            services.AddScoped<IUserService, UserService>();
          
            // RBAC Services
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<IRoleService, RoleService>();

            // Portfolio Services
            services.AddScoped<IPortfolioService, PortfolioService>();

            // GitHub Services
            services.AddScoped<IGitHubRepositoryService, GitHubRepositoryService>();
            services.AddScoped<IGitHubApiClient, GitHubApiClient>();

            // Chatbot, RAG Services
            services.AddScoped<IResourceService, ResourceService>();
            services.AddScoped<IChatService, ChatService>();

            services.AddSingleton<IRagService, RagService>();


            return services;
        }
    }
}