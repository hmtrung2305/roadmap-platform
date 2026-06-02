using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pgvector.EntityFrameworkCore;
using RoadmapPlatform.Application.Interfaces;
using RoadmapPlatform.Infrastructure.Configurations;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services;

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

            services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

            // Đăng ký implementation cho external services ở đây sau.
            // Ví dụ:

            // Authentication Services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IOAuthLoginService, OAuthLoginService>();
            
            // User Services
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<IRoleService, RoleService>();
            
            
            // services.AddScoped<IEmailSender, EmailSender>();
            // services.AddScoped<IGitHubClient, GitHubApiClient>();
            // services.AddScoped<IRagService, RagService>();
            // services.AddScoped<IJobMarketAnalysisClient, JobMarketAnalysisClient>();

            return services;
        }
    }
}