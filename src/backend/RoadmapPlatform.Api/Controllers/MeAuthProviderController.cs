using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RoadmapPlatform.Application.DTOs.AuthProviders;
using RoadmapPlatform.Application.Interfaces.Auth;
using System.Security.Claims;

namespace RoadmapPlatform.Api.Controllers
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
        [Authorize]
        public async Task<IActionResult> LinkLocalLogin(LinkLocalLoginRequestDto request)
        {
            var userId = GetCurrentUserId();

            await _authProviderService.LinkLocalLoginAsync(userId, request);

            return Ok(new
            {
                message = "Password login added. Verification code sent to email."
            });
        }

        [HttpPost("local/resend-verification")]
        [Authorize]
        public async Task<IActionResult> ResendLinkedLocalVerification()
        {
            var userId = GetCurrentUserId();

            await _emailVerificationService.ResendLinkedLocalVerificationAsync(userId);

            return Ok(new
            {
                message = "Verification code sent"
            });
        }

        [HttpPost("local/verify")]
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
        [Authorize]
        public async Task<IActionResult> RequestLocalEmailChange(UpdateLocalEmailRequestDto request)
        {
            var userId = GetCurrentUserId();

            await _emailVerificationService.RequestLocalEmailChangeAsync(userId, request.NewEmail!);

            return Ok(new
            {
                message = "Verification code sent"
            });
        }

        [HttpPost("local/email/verify")]
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
        [AllowAnonymous]
        public async Task<IActionResult> LinkGitHubCallback()
        {
            var result = await HttpContext.AuthenticateAsync(ExternalScheme);

            if (!result.Succeeded || result.Principal == null)
            {
                return Unauthorized("GitHub authentication failed");
            }

            if (!TryGetLinkingUserId(result.Properties, out var userId))
            {
                await HttpContext.SignOutAsync(ExternalScheme);
                return Unauthorized("GitHub linking session was invalid or expired");
            }

            await _authProviderService.LinkGitHubAsync(userId, result.Principal);

            await HttpContext.SignOutAsync(ExternalScheme);

            return Redirect(BuildFrontendUrl("/dashboard"));
        }

        [HttpGet("google/link")]
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
        [AllowAnonymous]
        public async Task<IActionResult> LinkGoogleCallback()
        {
            var result = await HttpContext.AuthenticateAsync(ExternalScheme);

            if (!result.Succeeded || result.Principal == null)
            {
                return Unauthorized("Google authentication failed");
            }

            if (!TryGetLinkingUserId(result.Properties, out var userId))
            {
                await HttpContext.SignOutAsync(ExternalScheme);
                return Unauthorized("Google linking session was invalid or expired");
            }

            await _authProviderService.LinkGoogleAsync(userId, result.Principal);

            await HttpContext.SignOutAsync(ExternalScheme);

            return Redirect(BuildFrontendUrl("/dashboard"));
        }

        [HttpDelete("{provider}")]
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