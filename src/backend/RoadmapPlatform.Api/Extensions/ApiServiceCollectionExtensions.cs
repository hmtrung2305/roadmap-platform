using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RoadmapPlatform.Api.Constants;
using RoadmapPlatform.Api.Responses;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace RoadmapPlatform.Api.Extensions
{
    // Class này dùng để đăng ký các service chung thuộc tầng API.
    // Những phần nên đặt ở đây gồm: Controllers, CORS, HttpContextAccessor,
    // cấu hình JSON, cấu hình model validation, và các setup API chung.
    // Không đặt cấu hình Authentication/Authorization lớn ở đây để tránh file bị phình to.
    public static class ApiServiceCollectionExtensions
    {
        public static IServiceCollection AddApiServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var allowedOrigins = configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? [];

            services.AddHttpClient();

            services.AddResponseCompression();

            services.AddControllers()
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.InvalidModelStateResponseFactory = context =>
                    {
                        var response = ApiErrorResponseFactory.FromModelState(
                            context.HttpContext,
                            context.ModelState);

                        return new BadRequestObjectResult(response);
                    };
                });

            services.AddHttpContextAccessor();

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = async (context, cancellationToken) =>
                {
                    var httpContext = context.HttpContext;
                    httpContext.Response.ContentType = "application/json";

                    TimeSpan? retryAfter = null;
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue))
                    {
                        retryAfter = retryAfterValue;
                        httpContext.Response.Headers["Retry-After"] = Math.Ceiling(retryAfterValue.TotalSeconds).ToString();
                    }

                    var retryAfterSeconds = retryAfter.HasValue
                        ? (int)Math.Ceiling(retryAfter.Value.TotalSeconds)
                        : (int?)null;

                    var response = ApiErrorResponseFactory.Create(
                        httpContext,
                        StatusCodes.Status429TooManyRequests,
                        "RATE_LIMIT_EXCEEDED",
                        "Too many requests. Please try again later.",
                        retryAfterSeconds: retryAfterSeconds);

                    httpContext.Response.StatusCode = response.Status;

                    await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
                };

                options.AddPolicy(RateLimitPolicyNames.AuthStrict, context =>
                    CreateFixedWindowPartition(
                        context,
                        RateLimitPolicyNames.AuthStrict,
                        permitLimit: 5,
                        window: TimeSpan.FromMinutes(1)));

                options.AddPolicy(RateLimitPolicyNames.AiExpensive, context =>
                    CreateFixedWindowPartition(
                        context,
                        RateLimitPolicyNames.AiExpensive,
                        permitLimit: 5,
                        window: TimeSpan.FromMinutes(1)));

                options.AddPolicy(RateLimitPolicyNames.UploadExpensive, context =>
                    CreateFixedWindowPartition(
                        context,
                        RateLimitPolicyNames.UploadExpensive,
                        permitLimit: 3,
                        window: TimeSpan.FromMinutes(1)));

                options.AddPolicy(RateLimitPolicyNames.AdminMutation, context =>
                    CreateFixedWindowPartition(
                        context,
                        RateLimitPolicyNames.AdminMutation,
                        permitLimit: 30,
                        window: TimeSpan.FromMinutes(1)));
            });

            services.AddCors(options =>
            {
                options.AddPolicy("DefaultCorsPolicy", policy =>
                {
                    policy
                        .WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            return services;
        }

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
                    PermitLimit = permitLimit,
                    Window = window,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    AutoReplenishment = true
                });
        }

        private static string GetPartitionKey(HttpContext context, string policyName)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(userId))
            {
                return $"{policyName}:user:{userId}";
            }

            return $"{policyName}:ip:{GetClientIp(context)}";
        }

        private static string GetClientIp(HttpContext context)
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                return forwardedFor.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault()
                    ?? "unknown";
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}
