using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RoadmapPlatform.Api.Responses;

namespace RoadmapPlatform.Api.Extensions
{
    /// <summary>
    /// Provides extension methods for configuring API authentication.
    /// </summary>
    /// <remarks>
    /// This class is reponsible for registering authentication schemes used by the API.
    ///
    /// It configures:
    /// - JWT Bearer authentication.
    /// - Reading JWT access tokens from cookies.
    /// - Standard JSON responses for 401 and 403 errors.
    /// - Temporary external login cookie.
    /// - Google OAuth authentication.
    /// - Github OAuth authentication.
    ///
    /// Authentication answer the question: "Who is the current user?"
    /// Authorization is configured separately and answers: "What is the user allowed to do?".
    /// </remarks>
    public static class ApiAuthenticationExtensions
    {
        /// <summary>
        /// Registers authentication schemes for the API.
        /// </summary>
        /// <param name="services">
        /// The dependency injection service collection.
        /// </param>
        /// <param name="configuration">
        /// The Application configuration source.
        /// Required values are read from appsettings, environment variables, or deployment secrets.
        /// </param>
        /// <returns>
        /// The same <see cref="IServiceCollection"/> instance so service registration can be chained.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a required authentication configuration value is missing.
        /// </exception>
        /// <remarks>
        /// This method configures JWT as the default authentication scheme.
        ///
        /// It also configures Google and Github OAuth for external login flows.
        /// The external OAuth result is temporarily stored in the "External" cookie scheme
        /// before the application creates its own local authentication session/token.
        /// </remarks>
        public static IServiceCollection AddApiAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services
                .AddAuthentication(options =>
                {
                    // Use JWT Bearer as the default scheme for authentication API requests.
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;

                    // Use JWT Bearer as the default scheme when authentication is requests.
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })

                // Configure JWT authentication for API requests.
                .AddJwtBearer(options =>
                {
                    // Define how incoming JWT tokens should be validated.
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        // Validate that the token was issued by the expected issuer.
                        ValidateIssuer = true,

                        // Validate that the token is intended for the expected audience.
                        ValidateAudience = true,

                        // Validate that the token has not expired.
                        ValidateLifetime = true,

                        // Validate that the token signature matches the configured signing key.
                        ValidateIssuerSigningKey = true,

                        // Expected token issuer.
                        ValidIssuer = GetRequiredConfig(configuration, "Jwt:Issuer"),

                        // Expected token audience.
                        ValidAudience = GetRequiredConfig(configuration, "Jwt:Audience"),

                        // Symmetric signing key used to validate the JWT signature.
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(GetRequiredConfig(configuration, "Jwt:Key")))
                    };

                    // Customize how JWT Bearer authentication handles tokens and errors.
                    options.Events = new JwtBearerEvents
                    {
                        // Read the JWT access token from the "access_token" cookie.
                        // This project uses cookie-based JWT transport instead of required
                        // the frontend to send an Authorization header manually.
                        OnMessageReceived = context =>
                        {
                            context.Token = context.Request.Cookies["access_token"];
                            return Task.CompletedTask;
                        },

                        // Return a standard JSON response when authentication is required
                        // but the request does not contain a valid authenticated user.
                        OnChallenge = async context =>
                        {
                            if (context.Response.HasStarted)
                            {
                                return;
                            }

                            // Prevent the default JWT challenge response.
                            context.HandleResponse();

                            var response = ApiErrorResponseFactory.Create(
                                context.HttpContext,
                                StatusCodes.Status401Unauthorized,
                                "UNAUTHORIZED",
                                "Authentication is required.");

                            context.Response.StatusCode = response.Status;
                            context.Response.ContentType = "application/json";

                            await context.Response.WriteAsJsonAsync(response);
                        },

                        // Return a standard JSON response when the user is authenticated
                        // but does not have permission to access the requested resource.
                        OnForbidden = async context =>
                        {
                            if (context.Response.HasStarted)
                            {
                                return;
                            }

                            var response = ApiErrorResponseFactory.Create(
                                context.HttpContext,
                                StatusCodes.Status403Forbidden,
                                "FORBIDDEN",
                                "You do not have permission to access this resource.");

                            context.Response.StatusCode = response.Status;
                            context.Response.ContentType = "application/json";

                            await context.Response.WriteAsJsonAsync(response);
                        }
                    };
                })

                // Register a temporary cookie scheme used during external OAuth login flows.
                // Google/Github login data is stored here before being converted
                // into the application's own authentication token.
                .AddCookie("External")

                // Configure Google OAuth login.
                .AddGoogle(options =>
                {
                    // Google OAuth application client ID.
                    options.ClientId = GetRequiredConfig(configuration, "Authentication:Google:ClientId");

                    // Google OAuth application client secret.
                    options.ClientSecret = GetRequiredConfig(configuration, "Authentication:Google:ClientSecret");

                    // Callback path that Google redirects to after login.
                    options.CallbackPath = "/signin-google";

                    // Store the external login result in the temporary external cookie.
                    options.SignInScheme = "External";
                })

                // Configure GitHub OAuth login.
                .AddGitHub(options =>
                {
                    // GitHub OAuth application client ID
                    options.ClientId = GetRequiredConfig(configuration, "Authentication:GitHub:ClientId");

                    // GitHub OAuth application client secret.
                    options.ClientSecret = GetRequiredConfig(configuration, "Authentication:GitHub:ClientSecret");

                    // Callback path that GitHub redirects to after login.
                    options.CallbackPath = "/signin-github";

                    // Store the external login result in the temporary external cookie.
                    options.SignInScheme = "External";

                    // Request access to the user's GitHub email addresses.
                    options.Scope.Add("user:email");

                    // Save the GitHub OAuth access token in the authentication ticket.
                    // This allows the callback flow to reuse the token for GitHub API calls.
                    options.SaveTokens = true;
                });

            return services;
        }

        /// <summary>
        /// Reads a required configuration value.
        /// </summary>
        /// <param name="configuration">
        /// The application configuration source.
        /// </param>
        /// <param name="key">
        /// The configuration key to read.
        /// </param>
        /// <returns>
        /// The configuration value for the given key.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the configuration value is missing.
        /// </exception>
        /// <remarks>
        /// This helper makes startup fail fast when required authentication settings
        /// are not configured correctly.
        ///
        /// This is useful because authentication should not run with missing issuer,
        /// audience, signing key, or OAuth credentials.
        /// </remarks>
        private static string GetRequiredConfig(IConfiguration configuration, string key)
        {
            return configuration[key]
                ?? throw new InvalidOperationException($"Missing configuration value: {key}");
        }
    }
}