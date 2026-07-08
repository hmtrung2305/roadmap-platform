using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using RoadmapPlatform.Api.Constants;
using RoadmapPlatform.Api.Extensions;
using RoadmapPlatform.Api.Responses;
using RoadmapPlatform.Application.DTOs.AuthProviders;
using RoadmapPlatform.Application.Interfaces.Auth;

namespace RoadmapPlatform.Api.Controllers.Auth
{
    /// <summary>
    /// Provides endpoints for managing authentication providers of the current user.
    /// </summary>
    /// <remarks>
    /// This controller handles account-level provider management, such as:
    /// - Viewing linked login providers.
    /// - Linking local email/password login.
    /// - Verifying linked local email.
    /// - Changing local email.
    /// - Changing local password.
    /// - Linking Google or GitHub login.
    /// - Unlinking a login provider.
    ///
    /// Business logic is delegated to IAuthProviderService and IEmailVerificationService.
    /// </remarks>
    [ApiController]
    [Route("api/me/auth-providers")]
    public class MeAuthProviderController : ControllerBase
    {
        private const string ExternalScheme = "External";
        private const string LinkingUserIdProperty = "linking_user_id";
        private const string ReturnUrlProperty = "frontend_return_url";
        private const string DefaultAccountSettingsPath = "/settings/account";

        private readonly IAuthProviderService _authProviderService;
        private readonly IEmailVerificationService _emailVerificationService;
        private readonly string _frontendBaseUrl;

        /// <summary>
        /// Creates a new current-user authentication provider controller.
        /// </summary>
        /// <param name="authProviderService">
        /// The service used to manage linked authentication providers.
        /// </param>
        /// <param name="emailVerificationService">
        /// The service used to handle email verification flows.
        /// </param>
        /// <param name="configuration">
        /// The application configuration used to read the frontend base URL.
        /// </param>
        public MeAuthProviderController(
            IAuthProviderService authProviderService,
            IEmailVerificationService emailVerificationService,
            IConfiguration configuration)
        {
            _authProviderService = authProviderService;
            _emailVerificationService = emailVerificationService;
            _frontendBaseUrl = NormalizeFrontendBaseUrl(configuration["Frontend:BaseUrl"]);
        }

        /// <summary>
        /// Gets the linked authentication provider status of the current user.
        /// </summary>
        /// <returns>
        /// The current user's authentication provider status.
        /// </returns>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAuthProviders()
        {
            var userId = GetCurrentUserId();

            var response = await _authProviderService.GetAuthProviderStatusAsync(userId);

            return Ok(response);
        }

        /// <summary>
        /// Links local email/password login to the current user's account.
        /// </summary>
        /// <param name="request">
        /// The local login linking request.
        /// </param>
        /// <returns>
        /// The result of the local login linking operation.
        /// </returns>
        [HttpPost("local")]
        [EnableRateLimiting(RateLimitPolicyNames.AuthStrict)]
        [Authorize]
        public async Task<IActionResult> LinkLocalLogin(LinkLocalLoginRequestDto request)
        {
            var userId = GetCurrentUserId();

            var response = await _authProviderService.LinkLocalLoginAsync(userId, request);

            return Ok(response);
        }

        /// <summary>
        /// Resends the verification code for the current user's linked local email.
        /// </summary>
        /// <param name="cancellationToken">
        /// A token used to cancel the request.
        /// </param>
        /// <returns>
        /// A success message when the verification code is sent.
        /// </returns>
        [HttpPost("local/resend-verification")]
        [EnableRateLimiting(RateLimitPolicyNames.AuthStrict)]
        [Authorize]
        public async Task<IActionResult> ResendLinkedLocalVerification(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            await _emailVerificationService.ResendLinkedLocalVerificationAsync(userId, cancellationToken);

            return Ok(new
            {
                message = "Verification code sent"
            });
        }

        /// <summary>
        /// Verifies the current user's linked local email.
        /// </summary>
        /// <param name="request">
        /// The OTP verification request.
        /// </param>
        /// <returns>
        /// A success message when the local login email is verified.
        /// </returns>
        [HttpPost("local/verify")]
        [EnableRateLimiting(RateLimitPolicyNames.AuthStrict)]
        [Authorize]
        public async Task<IActionResult> VerifyLinkedLocalEmail(VerifyOtpRequestDto request)
        {
            var userId = GetCurrentUserId();

            await _emailVerificationService.VerifyLinkedLocalEmailAsync(userId, request.Otp!);

            return Ok(new
            {
                message = "Local login email verified successfully"
            });
        }

        /// <summary>
        /// Requests a local login email change for the current user.
        /// </summary>
        /// <param name="request">
        /// The local email change request.
        /// </param>
        /// <returns>
        /// The email change verification response.
        /// </returns>
        [HttpPost("local/email/change-request")]
        [EnableRateLimiting(RateLimitPolicyNames.AuthStrict)]
        [Authorize]
        public async Task<IActionResult> RequestLocalEmailChange(UpdateLocalEmailRequestDto request)
        {
            var userId = GetCurrentUserId();

            var response = await _emailVerificationService.RequestLocalEmailChangeAsync(
                userId,
                request.NewEmail!);

            return Ok(response);
        }

        /// <summary>
        /// Resends the verification code for the current user's pending local email change.
        /// </summary>
        /// <param name="cancellationToken">
        /// A token used to cancel the request.
        /// </param>
        /// <returns>
        /// A success message when the verification code is sent.
        /// </returns>
        [HttpPost("local/email/resend-verification")]
        [EnableRateLimiting(RateLimitPolicyNames.AuthStrict)]
        [Authorize]
        public async Task<IActionResult> ResendLocalEmailChangeVerification(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            await _emailVerificationService.ResendLocalEmailChangeVerificationAsync(userId, cancellationToken);

            return Ok(new
            {
                message = "Verification code sent"
            });
        }

        /// <summary>
        /// Verifies and applies the current user's pending local email change.
        /// </summary>
        /// <param name="request">
        /// The OTP verification request.
        /// </param>
        /// <returns>
        /// A success message when the email is changed.
        /// </returns>
        [HttpPost("local/email/verify")]
        [EnableRateLimiting(RateLimitPolicyNames.AuthStrict)]
        [Authorize]
        public async Task<IActionResult> VerifyLocalEmailChange(VerifyOtpRequestDto request)
        {
            var userId = GetCurrentUserId();

            await _emailVerificationService.VerifyLocalEmailChangeAsync(userId, request.Otp!);

            return Ok(new
            {
                message = "Email changed successfully"
            });
        }

        /// <summary>
        /// Changes the current user's local password.
        /// </summary>
        /// <param name="request">
        /// The password change request.
        /// </param>
        /// <returns>
        /// A success message when the password is changed.
        /// </returns>
        [HttpPut("local/password")]
        [EnableRateLimiting(RateLimitPolicyNames.AuthStrict)]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequestDto request)
        {
            var userId = GetCurrentUserId();

            await _authProviderService.ChangePasswordAsync(userId, request);

            return Ok(new
            {
                message = "Password changed successfully"
            });
        }

        /// <summary>
        /// Starts the GitHub linking flow for the current user's account.
        /// </summary>
        /// <param name="returnUrl">
        /// Optional frontend return path after linking is completed.
        /// </param>
        /// <returns>
        /// A challenge response that redirects the user to GitHub authentication.
        /// </returns>
        [HttpGet("github/link")]
        [EnableRateLimiting(RateLimitPolicyNames.AuthStrict)]
        [Authorize]
        public IActionResult LinkGitHubLogin([FromQuery] string? returnUrl)
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(LinkGitHubCallback))
            };

            // Store the current user ID so the callback knows which account to link.
            properties.Items[LinkingUserIdProperty] = GetCurrentUserId().ToString();

            // Store a safe frontend return path for the final redirect.
            properties.Items[ReturnUrlProperty] = NormalizeFrontendReturnUrl(returnUrl);

            return Challenge(properties, GitHubAuthenticationDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// Handles the GitHub OAuth callback for account linking.
        /// </summary>
        /// <returns>
        /// Redirects back to the frontend account settings page or the provided safe return path.
        /// </returns>
        [HttpGet("github/callback")]
        [EnableRateLimiting(RateLimitPolicyNames.AuthStrict)]
        [AllowAnonymous]
        public async Task<IActionResult> LinkGitHubCallback()
        {
            // Read the temporary external OAuth result.
            var result = await HttpContext.AuthenticateAsync(ExternalScheme);

            if (!result.Succeeded || result.Principal == null)
            {
                return Unauthorized(ApiErrorResponseFactory.Create(
                    HttpContext,
                    StatusCodes.Status401Unauthorized,
                    "OAUTH_AUTHENTICATION_FAILED",
                    "GitHub authentication failed"));
            }

            if (!TryGetLinkingUserId(result.Properties, out var userId))
            {
                await HttpContext.SignOutAsync(ExternalScheme);
                return Unauthorized(ApiErrorResponseFactory.Create(
                    HttpContext,
                    StatusCodes.Status401Unauthorized,
                    "OAUTH_LINKING_SESSION_INVALID",
                    "GitHub linking session was invalid or expired"));
            }

            // Read the GitHub OAuth access token when available.
            var githubAccessToken = result.Properties?.GetTokenValue("access_token");

            // Link the GitHub identity to the original authenticated user.
            await _authProviderService.LinkGitHubAsync(
                userId,
                result.Principal,
                githubAccessToken);

            // Clear the temporary external OAuth cookie.
            await HttpContext.SignOutAsync(ExternalScheme);

            return Redirect(BuildFrontendUrl(GetFrontendReturnUrl(result.Properties)));
        }

        /// <summary>
        /// Starts the Google linking flow for the current user's account.
        /// </summary>
        /// <returns>
        /// A challenge response that redirects the user to Google authentication.
        /// </returns>
        [HttpGet("google/link")]
        [EnableRateLimiting(RateLimitPolicyNames.AuthStrict)]
        [Authorize]
        public IActionResult LinkGoogleLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(LinkGoogleCallback))
            };

            // Store the current user ID so the callback knows which account to link.
            properties.Items[LinkingUserIdProperty] = GetCurrentUserId().ToString();

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// Handles the Google OAuth callback for account linking.
        /// </summary>
        /// <returns>
        /// Redirects back to the frontend account settings page.
        /// </returns>
        [HttpGet("google/callback")]
        [EnableRateLimiting(RateLimitPolicyNames.AuthStrict)]
        [AllowAnonymous]
        public async Task<IActionResult> LinkGoogleCallback()
        {
            // Read the temporary external OAuth result.
            var result = await HttpContext.AuthenticateAsync(ExternalScheme);

            if (!result.Succeeded || result.Principal == null)
            {
                return Unauthorized(ApiErrorResponseFactory.Create(
                    HttpContext,
                    StatusCodes.Status401Unauthorized,
                    "OAUTH_AUTHENTICATION_FAILED",
                    "Google authentication failed"));
            }

            if (!TryGetLinkingUserId(result.Properties, out var userId))
            {
                await HttpContext.SignOutAsync(ExternalScheme);
                return Unauthorized(ApiErrorResponseFactory.Create(
                    HttpContext,
                    StatusCodes.Status401Unauthorized,
                    "OAUTH_LINKING_SESSION_INVALID",
                    "Google linking session was invalid or expired"));
            }

            // Link the Google identity to the original authenticated user.
            await _authProviderService.LinkGoogleAsync(userId, result.Principal);

            // Clear the temporary external OAuth cookie.
            await HttpContext.SignOutAsync(ExternalScheme);

            return Redirect(BuildFrontendUrl(DefaultAccountSettingsPath));
        }

        /// <summary>
        /// Unlinks an authentication provider from the current user's account.
        /// </summary>
        /// <param name="provider">
        /// The provider name to unlink.
        /// </param>
        /// <returns>
        /// A success message when the provider is removed.
        /// </returns>
        [HttpDelete("{provider}")]
        [EnableRateLimiting(RateLimitPolicyNames.AuthStrict)]
        [Authorize]
        public async Task<IActionResult> UnlinkProvider(string provider)
        {
            var userId = GetCurrentUserId();

            await _authProviderService.UnlinkProviderAsync(userId, provider);

            return Ok(new
            {
                message = $"{provider} login removed successfully"
            });
        }

        /// <summary>
        /// Gets the current authenticated user's ID from claims.
        /// </summary>
        /// <returns>
        /// The current user's ID.
        /// </returns>
        private Guid GetCurrentUserId()
        {
            return User.GetUserId();
        }

        /// <summary>
        /// Tries to read the original linking user ID from external authentication properties.
        /// </summary>
        /// <param name="properties">
        /// The external authentication properties.
        /// </param>
        /// <param name="userId">
        /// The parsed linking user ID.
        /// </param>
        /// <returns>
        /// True when the linking user ID exists and is a valid GUID; otherwise, false.
        /// </returns>
        private static bool TryGetLinkingUserId(AuthenticationProperties? properties, out Guid userId)
        {
            userId = Guid.Empty;

            if (properties == null)
            {
                return false;
            }

            if (!properties.Items.TryGetValue(LinkingUserIdProperty, out var linkingUserIdText))
            {
                return false;
            }

            return Guid.TryParse(linkingUserIdText, out userId);
        }

        /// <summary>
        /// Gets the frontend return path stored in external authentication properties.
        /// </summary>
        /// <param name="properties">
        /// The external authentication properties.
        /// </param>
        /// <returns>
        /// A safe frontend return path.
        /// </returns>
        private static string GetFrontendReturnUrl(AuthenticationProperties? properties)
        {
            if (properties?.Items.TryGetValue(ReturnUrlProperty, out var returnUrl) == true)
            {
                return NormalizeFrontendReturnUrl(returnUrl);
            }

            return DefaultAccountSettingsPath;
        }

        /// <summary>
        /// Normalizes and validates a frontend return path.
        /// </summary>
        /// <param name="returnUrl">
        /// The requested frontend return path.
        /// </param>
        /// <returns>
        /// The requested path if it is safe; otherwise, the default account settings path.
        /// </returns>
        /// <remarks>
        /// This method only allows relative frontend paths that start with a single slash.
        /// It rejects absolute URLs, protocol-relative URLs, and paths containing backslashes
        /// to reduce open redirect risk.
        /// </remarks>
        private static string NormalizeFrontendReturnUrl(string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                return DefaultAccountSettingsPath;
            }

            var trimmed = returnUrl.Trim();

            if (!trimmed.StartsWith("/", StringComparison.Ordinal) ||
                trimmed.StartsWith("//", StringComparison.Ordinal) ||
                trimmed.Contains("://", StringComparison.Ordinal) ||
                trimmed.Contains('\\'))
            {
                return DefaultAccountSettingsPath;
            }

            return trimmed;
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