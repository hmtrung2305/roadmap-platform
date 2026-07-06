using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RoadmapPlatform.Application.Interfaces.AiCredits;
using RoadmapPlatform.Application.Interfaces.AiMentor;
using RoadmapPlatform.Application.Interfaces.Auth;
using RoadmapPlatform.Application.Interfaces.Roadmaps.ContentManagement;
using RoadmapPlatform.Application.Interfaces.GitHub;
using RoadmapPlatform.Application.Interfaces.Identity;
using RoadmapPlatform.Application.Interfaces.LearningModules;
using RoadmapPlatform.Application.Interfaces.LearningResources;
using RoadmapPlatform.Application.Interfaces.MarketPulse;
using RoadmapPlatform.Application.Interfaces.Portfolio;
using RoadmapPlatform.Application.Interfaces.Roadmaps;
using RoadmapPlatform.Application.Interfaces.Security;
using RoadmapPlatform.Application.Interfaces.Skills;
using RoadmapPlatform.Application.Interfaces.SkillGapAnalysis;
using RoadmapPlatform.Application.Interfaces.Storage;
using RoadmapPlatform.Application.Interfaces.Streaks;
using RoadmapPlatform.Application.Interfaces.Users;
using RoadmapPlatform.Infrastructure.Clients;
using RoadmapPlatform.Infrastructure.Configurations;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Security;
using RoadmapPlatform.Infrastructure.Services.AiCredits;
using RoadmapPlatform.Infrastructure.Services.AiMentor;
using RoadmapPlatform.Infrastructure.Services.Auth;
using RoadmapPlatform.Infrastructure.Services.Roadmaps.ContentManagement;
using RoadmapPlatform.Infrastructure.Services.GitHub;
using RoadmapPlatform.Infrastructure.Services.Identity;
using RoadmapPlatform.Infrastructure.Services.LearningModules;
using RoadmapPlatform.Infrastructure.Services.LearningResources;
using RoadmapPlatform.Infrastructure.Services.MarketPulse;
using RoadmapPlatform.Infrastructure.Services.Portfolio;
using RoadmapPlatform.Infrastructure.Services.Roadmaps;
using RoadmapPlatform.Infrastructure.Services.Security;
using RoadmapPlatform.Infrastructure.Services.Skills;
using RoadmapPlatform.Infrastructure.Services.SkillGapAnalysis;
using RoadmapPlatform.Infrastructure.Services.Storage;
using RoadmapPlatform.Infrastructure.Services.Streaks;
using RoadmapPlatform.Infrastructure.Services.Users;

namespace RoadmapPlatform.Infrastructure.Extensions
{
    /// <summary>
    /// Provides extension methods for registering infrastructure-layer services.
    /// </summary>
    /// <remarks>
    /// This class is the main dependency injection registration point for the Infrastructure layer.
    ///
    /// It registers:
    /// - Database context.
    /// - Configuration option bindings.
    /// - Authentication and identity services.
    /// - User, RBAC, portfolio, GitHub, roadmap, skill, skill gap, learning module, and AI services.
    /// - Market Pulse services and background jobs.
    /// - File storage providers.
    /// - Authorization handlers.
    /// - Cache services.
    ///
    /// This file should only wire dependencies together.
    /// Business logic should stay inside the actual service classes.
    /// </remarks>
    public static class InfrastructureServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all infrastructure service implementations into the dependency injection container.
        /// </summary>
        /// <param name="services">
        /// The dependency injection service collection.
        /// </param>
        /// <param name="configuration">
        /// The application configuration source.
        /// </param>
        /// <returns>
        /// The same <see cref="IServiceCollection"/> instance so registration calls can be chained.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the required database connection string is missing.
        /// </exception>
        /// <remarks>
        /// This method connects application-layer interfaces to infrastructure-layer implementations.
        ///
        /// Example:
        /// IAuthService is registered with AuthService.
        /// IMarketPulseService is registered with MarketPulseService.
        /// IFileStorage is resolved dynamically based on the configured storage provider.
        ///
        /// This method is called from the API startup flow and also reused by the Market Pulse job.
        /// </remarks>
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Read the main database connection string.
            var defaultConnection = configuration.GetConnectionString("DefaultConnection");

            // Fail fast if the database connection string is missing.
            // The application should never run without an explicitly configured database.
            if (string.IsNullOrWhiteSpace(defaultConnection))
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:DefaultConnection must be configured by environment variable, " +
                    "user secret, or deployment secret. Do not commit database credentials in appsettings.json.");
            }

            // Register the EF Core DbContext with PostgreSQL.
            // pgvector support is enabled through UseVector().
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(
                    defaultConnection,
                    npgsqlOptions =>
                    {
                        npgsqlOptions.UseVector();
                    }));

            // Bind JWT settings from configuration.
            services.Configure<JwtSettings>(
                configuration.GetSection("Jwt"));

            // Bind AI and RAG-related settings from configuration.
            services.Configure<LearningModuleRagSettings>(configuration.GetSection("Rag"));
            services.Configure<LearningModuleIndexingSettings>(
                configuration.GetSection(LearningModuleIndexingSettings.SectionName));
            services.Configure<AiSettings>(configuration.GetSection("Ai"));
            services.Configure<CaptchaSettings>(configuration.GetSection("Captcha"));
            services.Configure<MarketPulseSettings>(configuration.GetSection("MarketPulse"));

            // Normalize or apply environment-specific aliases for Market Pulse settings.
            services.PostConfigure<MarketPulseSettings>(settings => settings.ApplyEnvironmentAliases());

            // Bind file storage settings from configuration.
            services.Configure<FileStorageSettings>(configuration.GetSection(FileStorageSettings.SectionName));

            // Register authentication-related services.
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IOAuthLoginService, OAuthLoginService>();
            services.AddScoped<IAuthProviderService, AuthProviderService>();
            services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
            services.AddHttpClient<ICaptchaService, TurnstileCaptchaService>();

            // Register user-related services.
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAdminUserService, AdminUserService>();
            services.AddScoped<IAccountProfileService, AccountProfileService>();

            // Register RBAC services.
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<IRoleService, RoleService>();

            // Register portfolio services.
            services.AddScoped<IPortfolioService, PortfolioService>();

            // Register GitHub-related services.
            services.AddScoped<IGitHubRepositoryService, GitHubRepositoryService>();
            services.AddScoped<IRepoInsightService, RepoInsightService>();
            services.AddScoped<IRepoSummaryGenerator, AiRepoSummaryGenerator>();
            services.AddScoped<IGitHubApiClient, GitHubApiClient>();
            services.AddScoped<IGitHubTokenService, GitHubTokenService>();

            // Register AI credit tracking service.
            services.AddScoped<IAiCreditService, AiCreditService>();

            // Register learner streak service.
            services.AddScoped<IStreakService, StreakService>();

            // Register Market Pulse services.
            services.AddScoped<JobsApiClient>();
            services.AddScoped<IJobPortalScraper, JobPortalScraper>();
            services.AddScoped<IMarketPulseService, MarketPulseService>();
            services.AddScoped<IMarketPulseAdminService, MarketPulseAdminService>();

            // Register the hosted background service for scheduled Market Pulse refresh.
            services.AddHostedService<MarketPulseHostedService>();

            // Register a named HTTP client for Market Pulse external calls.
            services.AddHttpClient("market-pulse", client =>
            {
                // Read request timeout from configuration and clamp it to a safe range.
                var timeoutSeconds = Math.Clamp(
                    configuration.GetValue<int?>("MarketPulse:RequestTimeoutSeconds") ?? 30,
                    5,
                    120);

                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

                // Set a clear User-Agent for outbound Market Pulse requests.
                client.DefaultRequestHeaders.UserAgent.ParseAdd("RoadmapPlatform-MarketPulse/1.0");
            });

            // Register skill and catalog services.
            services.AddScoped<ISkillLookupService, SkillLookupService>();
            services.AddScoped<IContentSkillCatalogService, ContentSkillCatalogService>();
            services.AddScoped<IContentLearningResourceCatalogService, ContentLearningResourceCatalogService>();

            // Register learner-facing roadmap services.
            services.AddScoped<RoadmapDetailBuilder>();
            services.AddScoped<IRoadmapQueryService, RoadmapQueryService>();
            services.AddScoped<IRoadmapEnrollmentService, RoadmapEnrollmentService>();
            services.AddScoped<IRoadmapProgressService, RoadmapProgressService>();
            services.AddScoped<IRoadmapLayoutService, RoadmapLayoutService>();

            // Register content manager roadmap services.
            services.AddScoped<ContentManagerRoadmapQueryService>();
            services.AddScoped<ContentManagerRoadmapMetadataService>();
            services.AddScoped<ContentManagerRoadmapMappingService>();
            services.AddScoped<ContentManagerRoadmapStructureService>();
            services.AddScoped<ContentManagerRoadmapDraftService>();
            services.AddScoped<ContentManagerRoadmapValidationService>();
            services.AddScoped<ContentManagerLearningResourceSearchService>();
            services.AddScoped<IContentManagerRoadmapService, ContentManagerRoadmapService>();

            // Register available file storage implementations.
            services.AddScoped<LocalFileStorage>();
            services.AddHttpClient<SupabaseFileStorage>();

            // Resolve IFileStorage based on configured provider.
            services.AddScoped<IFileStorage>(serviceProvider =>
            {
                var settings = serviceProvider.GetRequiredService<IOptions<FileStorageSettings>>().Value;
                var provider = string.IsNullOrWhiteSpace(settings.Provider)
                    ? "Local"
                    : settings.Provider.Trim();

                if (provider.Equals("Supabase", StringComparison.OrdinalIgnoreCase))
                {
                    return serviceProvider.GetRequiredService<SupabaseFileStorage>();
                }

                if (provider.Equals("Local", StringComparison.OrdinalIgnoreCase))
                {
                    return serviceProvider.GetRequiredService<LocalFileStorage>();
                }

                throw new InvalidOperationException($"Unsupported file storage provider '{settings.Provider}'.");
            });

            // Register learning module services.
            services.AddScoped<LearningModuleMarkdownChunker>();
            services.AddScoped<ILearningModuleRagIndexingService, LearningModuleRagIndexingService>();
            services.AddScoped<IContentManagerLearningModuleService, ContentManagerLearningModuleService>();
            services.AddScoped<ILearningModuleLessonService, LearningModuleLessonService>();
            services.AddScoped<ILearningModuleQuizService, LearningModuleQuizService>();
            services.AddScoped<ILearnerLearningModuleService, LearnerLearningModuleService>();
            services.AddScoped<ILearningModuleChatService, LearningModuleChatService>();

            // Read whether background learning module indexing is enabled.
            var learningModuleIndexingEnabled =
                configuration.GetValue<bool?>($"{LearningModuleIndexingSettings.SectionName}:Enabled") ?? true;

            // Background indexing requires an AI API key.
            var aiApiKeyConfigured = !string.IsNullOrWhiteSpace(configuration.GetValue<string>("Ai:ApiKey"));

            // Register the learning module indexing worker only when indexing is enabled
            // and the AI API key is available.
            if (learningModuleIndexingEnabled && aiApiKeyConfigured)
            {
                services.AddHostedService<LearningModuleIndexingWorker>();
            }

            // Register memory cache used by infrastructure services.
            services.AddMemoryCache();

            // Register permission cache.
            services.AddScoped<IPermissionCache, PermissionCache>();

            // Register custom authorization handlers.
            services.AddScoped<IAuthorizationHandler, PermissionHandler>();
            services.AddScoped<IAuthorizationHandler, AnyPermissionHandler>();

            // Register skill gap analysis services.
            services.AddScoped<ISkillGapAnalysisService, SkillGapAnalysisService>();
            services.AddScoped<ISkillGapCatalogService, SkillGapCatalogService>();
            services.AddScoped<ISkillGapAssessmentService, SkillGapAssessmentService>();
            services.AddScoped<ISkillGapCategoryConfigService, SkillGapCategoryConfigService>();
            services.AddScoped<ISkillGapHistoryService, SkillGapHistoryService>();

            // Register AI mentor chat service.
            services.AddScoped<IAiMentorService, AiMentorService>();

            return services;
        }
    }
}
