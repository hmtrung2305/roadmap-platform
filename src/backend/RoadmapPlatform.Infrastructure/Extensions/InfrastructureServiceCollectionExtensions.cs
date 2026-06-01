using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Pgvector.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

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

            // Đăng ký implementation cho external services ở đây sau.
            // Ví dụ:
            // services.AddScoped<IEmailSender, EmailSender>();
            // services.AddScoped<IGitHubClient, GitHubApiClient>();
            // services.AddScoped<IRagService, RagService>();
            // services.AddScoped<IJobMarketAnalysisClient, JobMarketAnalysisClient>();

            return services;
        }
    }
}