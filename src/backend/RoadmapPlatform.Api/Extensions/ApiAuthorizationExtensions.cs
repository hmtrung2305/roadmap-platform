using Microsoft.AspNetCore.Authorization;
using RoadmapPlatform.Infrastructure.Security;

namespace RoadmapPlatform.Api.Extensions
{
    /// <summary>
    /// Provides extension methods for configuring API authorization.
    /// </summary>
    /// <remarks>
    /// Authorization answers the question: "What is the current user allowed to do?"
    ///
    /// This class configures:
    /// - The default authorization behavior for API endpoints.
    /// - Dynamic permission-based authorization policies.
    ///
    /// Authentication is configured separately in ApiAuthenticationExtensions.
    /// </remarks>
    public static class ApiAuthorizationExtensions
    {
        /// <summary>
        /// Registers authorization services for the API.
        /// </summary>
        /// <param name="services">
        /// The dependency injection service collection.
        /// </param>
        /// <returns>
        /// The same <see cref="IServiceCollection"/> instance so calls can be chained.
        /// </returns>
        /// <remarks>
        /// This method sets a fallback authorization policy that requires users
        /// to be authenticated by default.
        ///
        /// It also registers a custom authorization policy provider that can create
        /// permission policies dynamically from policy names.
        /// </remarks>
        public static IServiceCollection AddApiAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                // Require authentication by default for endpoints that do not define
                // their own authorization behavior.
                //
                // Endpoints that should be publicly accessible must explicitly use
                // [AllowAnonymous].
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();

                // Static authorization policies can be registered here if needed.
                //
                // Dynamic permission policies are handled by PermissionPolicyProvider.
                // That provider supports policy names with prefixes such as:
                // - Permission:
                // - PermissionAny:
            });

            // Register the custom policy provider used to build permission-based
            // authorization policies dynamically.
            services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

            return services;
        }
    }
}
