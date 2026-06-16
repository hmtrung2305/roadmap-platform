using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using RoadmapPlatform.Api.Constants;
using RoadmapPlatform.Api.Responses;
using RoadmapPlatform.Application.DTOs.AuthProviders;
using RoadmapPlatform.Application.Interfaces.Auth;
using System.Security.Claims;

namespace RoadmapPlatform.Api.Controllers.Auth
{
    [ApiController]
    [Route("api/me/auth-providers")]
    public class MeAuthProviderController : ControllerBase
    {
        private const string ExternalScheme = "External";
        private const string LinkingUserIdProperty = "linking_user_id";

        private readonly IAuthProviderService _authProviderService;
        private readonly IEmailVerificationService _emailVerificationService;
        private readonly string _frontendBaseUrl;

        public MeAuthProviderController(
            IAuthProviderService authProviderService,
            IEmailVerificationService emailVerificationService,
            IConfiguration configuration)
        {
            _authProviderService = authProviderService;
            _emailVerificationService = emailVerificationService;
            _frontendBaseUrl = NormalizeFrontendBaseUrl(configuration["Frontend:BaseUrl"]);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAuthProviders()
        {
            var userId = GetCurrentUserId();

            var response = await _authProviderService.GetAuthProviderStatusAsync(userId);

            return Ok(response);
        }

        [HttpPost("local")]
        [EnableRateLimiting(RateLimitPolicyNames.AuthStrict)]
        [Authorize]
        public async Task<IActionResult> LinkLocalLogin(LinkLocalLoginRequestDto request)
        {
            var userId = GetCurrentUserId();

            var response = await _authProviderService.LinkLocalLoginAsync(userId, request);

            return Ok(response);
        }

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

        [HttpGet("github/link")]
        [EnableRateLimiting(RateLimitPolicyNames.AuthStrict)]
        [Authorize]
        public IActionResult LinkGitHubLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(LinkGitHubCallback))
            };

            properties.Items[LinkingUserIdProperty] = GetCurrentUserId().ToString();

            return Challenge(properties, GitHubAuthenticationDefaults.AuthenticationScheme);
        }

        [HttpGet("github/callback")]
        [EnableRateLimiting(RateLimitPolicyNames.AuthStrict)]
        [AllowAnonymous]
        public async Task<IActionResult> LinkGitHubCallback()
        {
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

            var githubAccessToken = result.Properties?.GetTokenValue("access_token");

            await _authProviderService.LinkGitHubAsync(
                userId,
                result.Principal,
                githubAccessToken);

            await HttpContext.SignOutAsync(ExternalScheme);

            return Redirect(BuildFrontendUrl("/settings/account"));
        }

        [HttpGet("google/link")]
        [EnableRateLimiting(RateLimitPolicyNames.AuthStrict)]
        [Authorize]
        public IActionResult LinkGoogleLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(LinkGoogleCallback))
            };

            properties.Items[LinkingUserIdProperty] = GetCurrentUserId().ToString();

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google/callback")]
        [EnableRateLimiting(RateLimitPolicyNames.AuthStrict)]
        [AllowAnonymous]
        public async Task<IActionResult> LinkGoogleCallback()
        {
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

            await _authProviderService.LinkGoogleAsync(userId, result.Principal);

            await HttpContext.SignOutAsync(ExternalScheme);

            return Redirect(BuildFrontendUrl("/settings/account"));
        }

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

        private Guid GetCurrentUserId()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(currentUserId, out var userId))
            {
                throw new InvalidOperationException("Invalid user id");
            }

            return userId;
        }

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

        private string BuildFrontendUrl(string path)
        {
            return $"{_frontendBaseUrl}{path}";
        }

        private static string NormalizeFrontendBaseUrl(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "http://localhost:5173"
                : value.Trim().TrimEnd('/');
        }
    }
}