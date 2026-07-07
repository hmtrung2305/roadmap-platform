using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RoadmapPlatform.Api.Constants;
using RoadmapPlatform.Api.Filters;
using RoadmapPlatform.Api.Responses;
using RoadmapPlatform.Application.DTOs.Auth;
using RoadmapPlatform.Application.Interfaces.Auth;

namespace RoadmapPlatform.Api.Controllers.Auth
{
    /// <summary>
    /// Provides authentication endpoints for registration, login, email verification,
    /// external OAuth login, and logout.
    /// </summary>
    [Route("api/auth")]
    [ApiController]
    [EnableRateLimiting(RateLimitPolicyNames.AuthStrict)]
    public class AuthController : ControllerBase
    {
        private const string ExternalScheme = "External";
        private const string AccessTokenCookieName = "access_token";

        private readonly IAuthService _authService;
        private readonly string _frontendBaseUrl;

        /// <summary>
        /// Creates a new authentication controller.
        /// </summary>
        /// <param name="authService">
        /// The authentication service used to execute authentication use cases.
        /// </param>
        /// <param name="configuration">
        /// The application configuration used to read the frontend base URL.
        /// </param>
        public AuthController(IAuthService authService, IConfiguration configuration)
        {
            _authService = authService;
            _frontendBaseUrl = NormalizeFrontendBaseUrl(configuration["Frontend:BaseUrl"]);
        }

        /// <summary>
        /// Registers a new user account and sends an email verification code.
        /// </summary>
        /// <param name="request">
        /// The registration request payload.
        /// </param>
        /// <returns>
        /// HTTP 202 Accepted when registration is accepted and verification is required.
        /// </returns>
        [AllowAnonymous]
        [HttpPost("register")]
        [RequireCaptcha("register")]
        public async Task<IActionResult> Register(RegisterRequestDto request)
        {
            // Delegate registration logic to the authentication service.
            var response = await _authService.RegisterAsync(request);

            // Registration is accepted, but the user still needs to verify email.
            return StatusCode(StatusCodes.Status202Accepted, response);
        }

        /// <summary>
        /// Authenticates a user with email and password.
        /// </summary>
        /// <param name="request">
        /// The login request payload.
        /// </param>
        /// <returns>
        /// User information and a success message. The access token is stored in a cookie.
        /// </returns>
        [AllowAnonymous]
        [HttpPost("login")]
        [RequireCaptcha("login")]
        public async Task<IActionResult> Login(LoginRequestDto request)
        {
            // Validate credentials and create a login response.
            var loginResponse = await _authService.LoginAsync(request);

            // Store the JWT access token in an HttpOnly cookie.
            AppendAccessTokenCookie(loginResponse.AccessToken);

            return Ok(new
            {
                user = loginResponse.User,
                message = "Logged in successfully"
            });
        }

        /// <summary>
        /// Verifies a user's registration email using a verification code.
        /// </summary>
        /// <param name="request">
        /// The email verification request payload.
        /// </param>
        /// <returns>
        /// User information and a success message. The access token is stored in a cookie.
        /// </returns>
        [AllowAnonymous]
        [HttpPost("registration/verify-email")]
        public async Task<IActionResult> VerifyRegistrationEmail(
            VerifyRegistrationEmailRequestDto request)
        {
            // Verify the registration email and create a login session.
            var loginResponse = await _authService.VerifyRegistrationEmailAsync(request);

            // Store the JWT access token after successful email verification.
            AppendAccessTokenCookie(loginResponse.AccessToken);

            return Ok(new
            {
                user = loginResponse.User,
                message = "Email verified successfully"
            });
        }

        /// <summary>
        /// Resends the registration email verification code.
        /// </summary>
        /// <param name="request">
        /// The resend verification request payload.
        /// </param>
        /// <returns>
        /// A success message when the verification code is sent.
        /// </returns>
        [AllowAnonymous]
        [HttpPost("registration/resend-verification")]
        [RequireCaptcha("resend-registration-verification")]
        public async Task<IActionResult> ResendRegistrationVerification(
            ResendRegistrationVerificationRequestDto request)
        {
            // Delegate resend verification logic to the authentication service.
            await _authService.ResendRegistrationVerificationAsync(request);

            return Ok(new
            {
                message = "Verification code sent"
            });
        }

        /// <summary>
        /// Starts the Google OAuth login flow.
        /// </summary>
        /// <returns>
        /// A challenge response that redirects the user to Google authentication.
        /// </returns>
        [AllowAnonymous]
        [HttpGet("google/login")]
        public IActionResult GoogleLogin()
        {
            // After Google login completes, ASP.NET Core redirects back to GoogleCallback.
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(GoogleCallback))
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// Handles the Google OAuth callback.
        /// </summary>
        /// <returns>
        /// Redirects to the frontend roadmaps page on success,
        /// or redirects to the login page with an OAuth error on failure.
        /// </returns>
        [AllowAnonymous]
        [HttpGet("google/callback")]
        public async Task<IActionResult> GoogleCallback()
        {
            // Read the temporary external authentication result.
            var result = await HttpContext.AuthenticateAsync(ExternalScheme);

            if (!result.Succeeded || result.Principal == null)
            {
                return Unauthorized(ApiErrorResponseFactory.Create(
                    HttpContext,
                    StatusCodes.Status401Unauthorized,
                    "OAUTH_AUTHENTICATION_FAILED",
                    "Google authentication failed"));
            }

            try
            {
                // Convert the Google principal into an application login session.
                var loginResponse = await _authService.LoginWithGoogleAsync(result.Principal);

                // Clear the temporary external authentication cookie.
                await HttpContext.SignOutAsync(ExternalScheme);

                // Store the application JWT access token.
                AppendAccessTokenCookie(loginResponse.AccessToken);

                return Redirect(BuildFrontendUrl("/roadmaps"));
            }
            catch (Exception ex)
            {
                // Always clear the temporary external authentication cookie on failure.
                await HttpContext.SignOutAsync(ExternalScheme);

                return RedirectWithOAuthError(ex.Message);
            }
        }

        /// <summary>
        /// Starts the GitHub OAuth login flow.
        /// </summary>
        /// <returns>
        /// A challenge response that redirects the user to GitHub authentication.
        /// </returns>
        [AllowAnonymous]
        [HttpGet("github/login")]
        public IActionResult GitHubLogin()
        {
            // After GitHub login completes, ASP.NET Core redirects back to GitHubCallback.
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(GitHubCallback))
            };

            return Challenge(properties, GitHubAuthenticationDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// Handles the GitHub OAuth callback.
        /// </summary>
        /// <returns>
        /// Redirects to the frontend roadmaps page on success,
        /// or redirects to the login page with an OAuth error on failure.
        /// </returns>
        [AllowAnonymous]
        [HttpGet("github/callback")]
        public async Task<IActionResult> GitHubCallback()
        {
            // Read the temporary external authentication result.
            var result = await HttpContext.AuthenticateAsync(ExternalScheme);

            if (!result.Succeeded || result.Principal == null)
            {
                return Unauthorized(ApiErrorResponseFactory.Create(
                    HttpContext,
                    StatusCodes.Status401Unauthorized,
                    "OAUTH_AUTHENTICATION_FAILED",
                    "GitHub authentication failed"));
            }

            try
            {
                // Read the GitHub access token saved by the OAuth middleware.
                var githubAccessToken = result.Properties?.GetTokenValue("access_token");

                // Convert the GitHub principal into an application login session.
                var loginResponse = await _authService.LoginWithGitHubAsync(
                    result.Principal,
                    githubAccessToken);

                // Clear the temporary external authentication cookie.
                await HttpContext.SignOutAsync(ExternalScheme);

                // Store the application JWT access token.
                AppendAccessTokenCookie(loginResponse.AccessToken);

                return Redirect(BuildFrontendUrl("/roadmaps"));
            }
            catch (Exception ex)
            {
                // Always clear the temporary external authentication cookie on failure.
                await HttpContext.SignOutAsync(ExternalScheme);

                return RedirectWithOAuthError(ex.Message);
            }
        }

        /// <summary>
        /// Logs out the current user by deleting the access token cookie.
        /// </summary>
        /// <returns>
        /// A success message when logout is completed.
        /// </returns>
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // Remove the JWT access token cookie from the browser.
            Response.Cookies.Delete(AccessTokenCookieName, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddMinutes(60)
            });

            return Ok(new
            {
                message = "Logged out successfully"
            });
        }

        /// <summary>
        /// Appends the JWT access token to the response as an HttpOnly cookie.
        /// </summary>
        /// <param name="accessToken">
        /// The JWT access token returned by the authentication service.
        /// </param>
        private void AppendAccessTokenCookie(string accessToken)
        {
            Response.Cookies.Append(AccessTokenCookieName, accessToken, new CookieOptions
            {
                // Prevent JavaScript from reading the token directly.
                HttpOnly = true,

                // Require HTTPS for the cookie.
                Secure = true,

                // Allow the cookie to be sent in cross-site frontend/backend deployments.
                SameSite = SameSiteMode.None,

                // Set the access token cookie lifetime.
                Expires = DateTime.UtcNow.AddMinutes(60)
            });
        }

        /// <summary>
        /// Redirects the user back to the frontend login page with an OAuth error message.
        /// </summary>
        /// <param name="message">
        /// The OAuth error message to send to the frontend.
        /// </param>
        /// <returns>
        /// A redirect response to the frontend login page.
        /// </returns>
        private IActionResult RedirectWithOAuthError(string message)
        {
            // Encode the message before placing it in the query string.
            var encodedMessage = Uri.EscapeDataString(message);

            return Redirect(BuildFrontendUrl($"/login?oauthError={encodedMessage}"));
        }

        /// <summary>
        /// Builds an absolute frontend URL from a relative frontend path.
        /// </summary>
        /// <param name="path">
        /// The frontend route path.
        /// </param>
        /// <returns>
        /// The absolute frontend URL.
        /// </returns>
        private string BuildFrontendUrl(string path)
        {
            return $"{_frontendBaseUrl}{path}";
        }

        /// <summary>
        /// Normalizes the configured frontend base URL.
        /// </summary>
        /// <param name="value">
        /// The configured frontend base URL.
        /// </param>
        /// <returns>
        /// A normalized frontend base URL without a trailing slash.
        /// </returns>
        private static string NormalizeFrontendBaseUrl(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "http://localhost:5173"
                : value.Trim().TrimEnd('/');
        }
    }
}