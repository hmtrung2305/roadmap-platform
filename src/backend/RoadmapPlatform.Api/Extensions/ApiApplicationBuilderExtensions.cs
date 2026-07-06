using RoadmapPlatform.Api.Middleware;

namespace RoadmapPlatform.Api.Extensions
{
    /// <summary>
    /// Provides extension methods for configuring the API HTTP request pipeline.
    /// </summary>
    /// <remarks>
    /// This class defines the order in which incoming HTTP requests pass through middleware.
    ///
    /// Middleware order is important. Authentication must run before authorization,
    /// CORS must run before protected endpoints are executed, and controllers must be
    /// mapped after the required middleware is configured.
    /// </remarks>
    public static class ApiApplicationBuilderExtensions
    {
        /// <summary>
        /// Configures the API middleware pipeline.
        /// </summary>
        /// <param name="app">
        /// The ASP.NET Core web application instance.
        /// </param>
        /// <returns>
        /// The same <see cref="WebApplication"/> instance so pipeline configuration can be chained.
        /// </returns>
        /// <remarks>
        /// The pipeline configured here handles:
        /// - HTTPS redirection.
        /// - Response compression.
        /// - Global exception handling.
        /// - Endpoint routing.
        /// - CORS.
        /// - Authentication.
        /// - Rate limiting.
        /// - Authorization.
        /// - Static files.
        /// - Controller endpoint mapping.
        /// </remarks>
        public static WebApplication UseApiPipeline(this WebApplication app)
        {
            // Redirect HTTP requests to HTTPS.
            app.UseHttpsRedirection();

            // Compress supported responses to reduce payload size.
            app.UseResponseCompression();

            // Convert unhandled exceptions into standardized API error responses.
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            // Enable endpoint routing.
            app.UseRouting();

            // Apply the default CORS policy configured in AddApiServices.
            app.UseCors("DefaultCorsPolicy");

            // Authenticate the current request and populate HttpContext.User.
            app.UseAuthentication();

            // Apply configured rate limiting policies.
            app.UseRateLimiter();

            // Check whether the authenticated user is allowed to access the endpoint.
            app.UseAuthorization();

            // Serve static files from the default web root when available.
            app.UseStaticFiles();

            // Map attribute-routed API controllers.
            app.MapControllers();

            return app;
        }
    }
}