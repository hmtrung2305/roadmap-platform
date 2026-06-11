using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace RoadmapPlatform.Api.Extensions
{
    // Class này dùng để cấu hình Authentication cho API.
    // Những phần nên đặt ở đây gồm: JWT Bearer setup, Google OAuth, GitHub OAuth,
    // external login cookie, token validation, issuer, audience, signing key,
    // và các cấu hình xác thực người dùng.
    // Authentication trả lời câu hỏi: "Người dùng là ai?"
    public static class ApiAuthenticationExtensions
    {
        public static IServiceCollection AddApiAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })

                // Configure JWT authentication.
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true, // Checks if the token issuer matches the ValidIssuer.
                        ValidateAudience = true, // Checks if the token audience matches the ValidAudience.
                        ValidateLifetime = true, // Checks if the token has not expired.
                        ValidateIssuerSigningKey = true, // Checks if the token signature is valid.

                        ValidIssuer = GetRequiredConfig(configuration, "Jwt:Issuer"),
                        ValidAudience = GetRequiredConfig(configuration, "Jwt:Audience"),
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(GetRequiredConfig(configuration, "Jwt:Key")))
                    };

                    // Configure JWT to read the access token from cookies.
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            context.Token = context.Request.Cookies["access_token"];
                            return Task.CompletedTask;
                        }
                    };
                })

                // Temporary cookie for external OAuth login flows.
                .AddCookie("External")

                // Configure Google OAuth login.
                .AddGoogle(options =>
                {
                    options.ClientId = GetRequiredConfig(configuration, "Authentication:Google:ClientId");
                    options.ClientSecret = GetRequiredConfig(configuration, "Authentication:Google:ClientSecret");
                    options.CallbackPath = "/signin-google";
                    options.SignInScheme = "External";
                })

                // Configure GitHub OAuth login.
                .AddGitHub(options =>
                {
                    options.ClientId = GetRequiredConfig(configuration, "Authentication:GitHub:ClientId");
                    options.ClientSecret = GetRequiredConfig(configuration, "Authentication:GitHub:ClientSecret");
                    options.CallbackPath = "/signin-github";
                    options.SignInScheme = "External";
                    options.Scope.Add("user:email");

                    // Persist the GitHub OAuth access token in the external auth ticket
                    // so the callback can store it for authenticated GitHub API calls.
                    options.SaveTokens = true;
                });

            return services;
        }

        private static string GetRequiredConfig(IConfiguration configuration, string key)
        {
            return configuration[key]
                ?? throw new InvalidOperationException($"Missing configuration value: {key}");
        }
    }
}