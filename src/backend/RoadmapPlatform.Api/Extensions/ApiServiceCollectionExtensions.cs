using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RoadmapPlatform.Api.Constants;
using RoadmapPlatform.Api.Responses;

namespace RoadmapPlatform.Api.Extensions
{
    /// <summary>
    /// Provides extension methods for registering API-layer services.
    /// </summary>
    /// <remarks>
    /// This class should only contain API-level service registrations such as:
    /// controllers, CORS, response compression, HTTP context access, API behavior,
    /// and rate limiting.
    ///
    /// Authentication and authorization are configured in separate extension files
    /// to keep the startup configuration clean and easier to maintain.
    /// </remarks>
    public static class ApiServiceCollectionExtensions
    {
        /// <summary>
        /// Register common API-layer services into the dependency injection container.
        /// </summary>
        /// <param name="services">
        /// The service collection used by ASP.NET Core dependency injection.
        /// </param>
        /// <param name="configuration">
        /// The application configuration source, usually loaded from appsettings,
        /// environment variables, and command-line arguments.
        /// </param>
        /// <returns>
        /// The same <see cref="IServiceCollection"/> instance so calls can be chained.
        /// </returns>
        /// <remarks>
        /// This method configures:
        ///
        /// - HTTP client support.
        /// - Response compression.
        /// - MVC controllers.
        /// - Custom validation error responses.
        /// - HTTP context accessor.
        /// - Rate limiting policies.
        /// - CORS policy for frontend access.
        ///
        /// It does not configure authentication, authorization, database access,
        /// or business services. Those are registered in other extension files.
        /// </remarks>
        public static IServiceCollection AddApiServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Read allowed frontend origins from configuration.
            // It the section is missing, use an empty array to avoid null values.
            var allowedOrigins = configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? [];

            // Register IHttpClientFactory.
            // This is used services need to call external HTTP APIs.
            services.AddHttpClient();

            // Enable response compression middleware support.
            // The middleware itself is added later in the request pipeline.
            services.AddResponseCompression();

            // Register MVC controllers and customize automatic model validation errors.
            services.AddControllers()
                .ConfigureApiBehaviorOptions(options =>
                {
                    // Replace the default ASP.NET Core validation response
                    // with the project's standard API error response format.
                    options.InvalidModelStateResponseFactory = context =>
                    {
                        var response = ApiErrorResponseFactory.FromModelState(
                            context.HttpContext,
                            context.ModelState);

                        return new BadRequestObjectResult(response);
                    };
                });

            // Allow services to access the current HttpContext when needed.
            services.AddHttpContextAccessor();

            // Configure named rate limiting policies for different API areas.
            services.AddRateLimiter(options =>
            {
                // Return HTTP 429 when a request exceeds the rate limit.
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // Customize the JSON response returned when rate limiting reject a request.
                options.OnRejected = async (context, cancellationToken) =>
                {
                    var httpContext = context.HttpContext;
                    httpContext.Response.ContentType = "application/json";

                    TimeSpan? retryAfter = null;

                    // Read Retry-After metadata from the rate limiter if available.
                    // This tells the client how long to wait before trying again.
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue))
                    {
                        retryAfter = retryAfterValue;
                        httpContext.Response.Headers["Retry-After"] =
                            Math.Ceiling(retryAfterValue.TotalSeconds).ToString();
                    }

                    var retryAfterSeconds = retryAfter.HasValue
                        ? (int)Math.Ceiling(retryAfter.Value.TotalSeconds)
                        : (int?)null;

                    // Build a standard API error response for rate limit errors.
                    var response = ApiErrorResponseFactory.Create(
                        httpContext,
                        StatusCodes.Status429TooManyRequests,
                        "RATE_LIMIT_EXCEEDED",
                        "Too many requests. Please try again later.",
                        retryAfterSeconds: retryAfterSeconds);

                    httpContext.Response.StatusCode = response.Status;

                    await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
                };

                // Strict limit for authentication endpoints such as login/register.
                // This helps reduce brute-force attempts.
                options.AddPolicy(RateLimitPolicyNames.AuthStrict, context =>
                    CreateFixedWindowPartition(
                        context,
                        RateLimitPolicyNames.AuthStrict,
                        permitLimit: 5,
                        window: TimeSpan.FromMinutes(1)));

                // Limit expensive AI endpoints.
                // AI requests are usually costly and should be protected.
                options.AddPolicy(RateLimitPolicyNames.AiExpensive, context =>
                    CreateFixedWindowPartition(
                        context,
                        RateLimitPolicyNames.AiExpensive,
                        permitLimit: 5,
                        window: TimeSpan.FromMinutes(1)));

                // Limit upload endpoints.
                // Upload can consume storage, bandwidth, and processing resources.
                options.AddPolicy(RateLimitPolicyNames.UploadExpensive, context =>
                    CreateFixedWindowPartition(
                        context,
                        RateLimitPolicyNames.UploadExpensive,
                        permitLimit: 3,
                        window: TimeSpan.FromMinutes(1)));

                // Limit admin mutation endpoints.
                // Admin actions are allowed more frequently but still protected.
                options.AddPolicy(RateLimitPolicyNames.AdminMutation, context =>
                    CreateFixedWindowPartition(
                        context,
                        RateLimitPolicyNames.AdminMutation,
                        permitLimit: 30,
                        window: TimeSpan.FromMinutes(1)));

                // Limit endpoints that call external APIs.
                // This helps avoid overloading third-party services.
                options.AddPolicy(RateLimitPolicyNames.ExternalApi, context =>
                    CreateFixedWindowPartition(
                        context,
                        RateLimitPolicyNames.ExternalApi,
                        permitLimit: 10,
                        window: TimeSpan.FromMinutes(1)));
            });

            // Configure CORS for frontend applications.
            services.AddCors(options =>
            {
                options.AddPolicy("DefaultCorsPolicy", policy =>
                {
                    policy
                        // Only allow configured frontend origins.
                        .WithOrigins(allowedOrigins)
                        // Allow frontend requests to send custom headers.
                        .AllowAnyHeader()
                        // Allow coomon HTTP methods such as GET, POST, PUT, PATCH, DELETE.
                        .AllowAnyMethod()
                        // Allow cookies and credentials to be sent from the frontend.
                        .AllowCredentials();
                });
            });

            return services;
        }

        /// <summary>
        /// Creates a fixed-window rate limit partition for the current request.
        /// </summary>
        /// <param name="context">
        /// The current HTTP request context.
        /// </param>
        /// <param name="policyName">
        /// The name of the rate limiting policy.
        /// </param>
        /// <param name="permitLimit">
        /// The maximum number of requests allowed within the time window.
        /// </param>
        /// <param name="window">
        /// The time window used by the fixed-window limiter.
        /// </param>
        /// <returns>
        /// A rate limit partition identified by user ID or client IP.
        /// </returns>
        /// <remarks>
        /// Fixed-window rate limiting means each partition gets a specific number
        /// of requests per time window.
        ///
        /// Example:
        /// permitLimit = 5 and window = 1 minitue means the same user/IP can make
        /// at most 5 requests in one minute for that policy.
        ///
        /// This method uses <see cref="GetPartitionKey"/> to decide whether the
        /// request should be limited by authenticated user ID or by IP address.
        /// </remarks>
        private static RateLimitPartition<string> CreateFixedWindowPartition(
            HttpContext context,
            string policyName,
            int permitLimit,
            TimeSpan window)
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                GetPartitionKey(context, policyName),
                _ => new FixedWindowRateLimiterOptions
                {
                    // Maximum number of allowed requests in the window.
                    PermitLimit = permitLimit,
                    // Duration of the fixed time window.
                    Window = window,
                    // Do not queue rejected requests.
                    // If the limit is reached, reject immediately.
                    QueueLimit = 0,
                    // Queue order is configured but unused because QueueLimit is 0.
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    // Automatically reset available permits after each window.
                    AutoReplenishment = true
                });
        }

        /// <summary>
        /// Builds the partition key used by the rate limiter.
        /// </summary>
        /// <param name="context">
        /// The current HTTP request context.
        /// </param>
        /// <param name="policyName">
        /// The name of the rate limiting policy.
        /// </param>
        /// <returns>
        /// A unique partition key for rate limiting.
        /// </returns>
        /// <remarks>
        /// If the request belongs to an authenticated user, the limiter uses
        /// the user's ID. This means each logged-in user has their own limit.
        ///
        /// If the request is anonymouse, the limiter fails back to the client IP.
        /// This protected public endpoints such as login or register.
        /// </remarks>
        private static string GetPartitionKey(HttpContext context, string policyName)
        {
            // Prefer user-based limiting for authenticated requests.
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(userId))
            {
                return $"{policyName}:user:{userId}";
            }

            // Fall back to IP-based limiting for anonymous request.
            return $"{policyName}:ip:{GetClientIp(context)}";
        }

        /// <summary>
        /// Gets the best available client IP address for the current request.
        /// </summary>
        /// <param name="context">
        /// The current HTTP request context.
        /// </param>
        /// <returns>
        /// The detected client IP address, or "unknown" if it cannot be resolved.
        /// </returns>
        /// <remarks>
        /// When the application runs behind a proxy or hosting platform,
        /// the real client IP is often stored in the X-Forwarded-For header.
        ///
        /// If X-Forwarded-For contains multiple IP addresses, the first one
        /// is usually the original client IP.
        ///
        /// If the header is missing, the method falls back to the direct remote IP
        /// from the connection.
        /// </remarks>
        private static string GetClientIp(HttpContext context)
        {
            // Try to get the original client IP from proxy headers.
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                return forwardedFor.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault()
                    ?? "unknown";
            }

            // Fall back to the direct remote IP address.
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}
