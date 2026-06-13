using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RoadmapPlatform.Application.Interfaces;
using RoadmapPlatform.Application.Interfaces.AiCredits;
using RoadmapPlatform.Application.Interfaces.Auth;
using RoadmapPlatform.Application.Interfaces.GitHub;
using RoadmapPlatform.Application.Interfaces.Identity;
using RoadmapPlatform.Application.Interfaces.LearningModules;
using RoadmapPlatform.Application.Interfaces.MarketPulse;
using RoadmapPlatform.Application.Interfaces.Portfolio;
using RoadmapPlatform.Application.Interfaces.Roadmaps;
using RoadmapPlatform.Application.Interfaces.Security;
using RoadmapPlatform.Application.Interfaces.Streaks;
using RoadmapPlatform.Application.Interfaces.Users;
using RoadmapPlatform.Infrastructure.Clients;
using RoadmapPlatform.Infrastructure.Configurations;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Security;
using RoadmapPlatform.Infrastructure.Services;
using RoadmapPlatform.Infrastructure.Services.AiCredits;
using RoadmapPlatform.Infrastructure.Services.Auth;
using RoadmapPlatform.Infrastructure.Services.Email;
using RoadmapPlatform.Infrastructure.Services.GitHub;
using RoadmapPlatform.Infrastructure.Services.Identity;
using RoadmapPlatform.Infrastructure.Services.LearningModules;
using RoadmapPlatform.Infrastructure.Services.MarketPulse;
using RoadmapPlatform.Infrastructure.Services.Portfolio;
using RoadmapPlatform.Infrastructure.Services.Roadmaps;
using RoadmapPlatform.Infrastructure.Services.Security;
using RoadmapPlatform.Infrastructure.Services.Streaks;
using RoadmapPlatform.Infrastructure.Services.Users;

namespace RoadmapPlatform.Infrastructure.Extensions
{
    // Registers infrastructure implementations: DbContext, external clients,
    // storage, email, RAG, security, and hosted background services.
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Database connection
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

            // AI, RAG Settings
            services.Configure<LearningModuleRagSettings>(configuration.GetSection("LearningModuleRag"));
            services.Configure<AiSettings>(configuration.GetSection("Ai"));
            services.Configure<CaptchaSettings>(configuration.GetSection("Captcha"));
            services.Configure<MarketPulseSettings>(configuration.GetSection("MarketPulse"));

            // File Storage Settings
            services.Configure<LearningModuleFileStorageSettings>(configuration.GetSection("LearningModuleFileStorage"));

            // Register external service implementations below.

            // Authentication Services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IOAuthLoginService, OAuthLoginService>();
            services.AddScoped<IAuthProviderService, AuthProviderService>();
            services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
            services.AddHttpClient<ICaptchaService, TurnstileCaptchaService>();

            // User Services
            services.AddScoped<IUserService, UserService>();

            // RBAC Services
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<IRoleService, RoleService>();

            // Portfolio Services
            services.AddScoped<IPortfolioService, PortfolioService>();

            // GitHub Services
            services.AddScoped<IGitHubRepositoryService, GitHubRepositoryService>();
            services.AddScoped<IRepoInsightService, RepoInsightService>();
            services.AddScoped<IRepoSummaryGenerator, AiRepoSummaryGenerator>();
            services.AddScoped<IGitHubApiClient, GitHubApiClient>();

            services.AddScoped<IAiCreditService, AiCreditService>();

            // Streak Service
            services.AddScoped<IStreakService, StreakService>();

            // Job market / Market Pulse Services
            services.AddScoped<JobsApiClient>();
            services.AddScoped<IJobMarketSnapshotProvider, JobsApiJobMarketSnapshotProvider>();
            services.AddScoped<IJobPortalScraper, JobPortalScraper>();
            services.AddScoped<IMarketPulseService, MarketPulseService>();
            services.AddHostedService<MarketPulseHostedService>();
            services.AddHttpClient("market-pulse", client =>
            {
                var timeoutSeconds = Math.Clamp(
                    configuration.GetValue<int?>("MarketPulse:RequestTimeoutSeconds") ?? 30,
                    5,
                    120);

                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("RoadmapPlatform-MarketPulse/1.0");
            });

            // Roadmap Services
            services.AddScoped<RoadmapDetailBuilder>();
            services.AddScoped<IRoadmapQueryService, RoadmapQueryService>();
            services.AddScoped<IRoadmapEnrollmentService, RoadmapEnrollmentService>();
            services.AddScoped<IRoadmapProgressService, RoadmapProgressService>();
            services.AddScoped<IRoadmapLayoutService, RoadmapLayoutService>();

            // Learning Module Services
            services.AddScoped<LearningModuleMarkdownChunker>();
            services.AddScoped<ILearningModuleFileStorage, LocalLearningModuleFileStorage>();
            services.AddScoped<ILearningModuleRagIndexingService, LearningModuleRagIndexingService>();
            services.AddScoped<ICounselorLearningModuleService, CounselorLearningModuleService>();

            // Cache memory
            services.AddMemoryCache();
            services.AddScoped<IPermissionCache, PermissionCache>();

            // Authorize Handler
            services.AddScoped<IAuthorizationHandler, PermissionHandler>();

            return services;
        }
    }
}
