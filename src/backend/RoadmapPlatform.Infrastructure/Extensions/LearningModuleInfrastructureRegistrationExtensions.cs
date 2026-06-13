using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RoadmapPlatform.Application.Interfaces.LearningModules;
using RoadmapPlatform.Infrastructure.Configurations;
using RoadmapPlatform.Infrastructure.Services.LearningModules;

namespace RoadmapPlatform.Infrastructure.Extensions;

public static class LearningModuleInfrastructureRegistrationExtensions
{
    public static IServiceCollection AddLearningModuleInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<LearningModuleFileStorageSettings>(
            configuration.GetSection("LearningModuleFileStorage"));

        services.Configure<LearningModuleRagSettings>(
            configuration.GetSection("LearningModuleRag"));

        services.AddScoped<LearningModuleMarkdownChunker>();
        services.AddScoped<ILearningModuleFileStorage, LocalLearningModuleFileStorage>();
        services.AddScoped<ILearningModuleRagIndexingService, LearningModuleRagIndexingService>();

        return services;
    }
}
