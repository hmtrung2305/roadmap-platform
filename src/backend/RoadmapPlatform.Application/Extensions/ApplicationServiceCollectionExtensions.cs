using Microsoft.Extensions.DependencyInjection;
using RoadmapPlatform.Application.Services.MarketPulse;

namespace RoadmapPlatform.Application.Extensions
{
    /// <summary>
    /// Provides extension methods for registering application-layer services.
    /// </summary>
    /// <remarks>
    /// The Application layer should contain framework-independent application logic,
    /// use-case services, DTO-related helpers, and pure business processing services.
    ///
    /// This file should only wire application-layer dependencies into the DI container.
    /// Infrastructure implementations such as database access, external API clients,
    /// file storage, and hosted workers should be registered in the Infrastructure layer.
    /// </remarks>
    public static class ApplicationServiceCollectionExtensions
    {
        /// <summary>
        /// Registers application-layer services into the dependency injection container.
        /// </summary>
        /// <param name="services">
        /// The dependency injection service collection.
        /// </param>
        /// <returns>
        /// The same <see cref="IServiceCollection"/> instance so registration calls can be chained.
        /// </returns>
        /// <remarks>
        /// The services registered here should be safe to reuse without depending on
        /// HTTP context, EF Core DbContext, external APIs, or deployment-specific resources.
        ///
        /// Current registrations are Market Pulse analysis helpers. They are registered
        /// as singletons because they are expected to be stateless processing services.
        /// </remarks>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Register stateless Market Pulse keyword analysis logic.
            services.AddSingleton<JobMarketKeywordAnalyzer>();

            // Register stateless Market Pulse overview building logic.
            services.AddSingleton<JobMarketOverviewBuilder>();
            
            return services;
        }
    }
}