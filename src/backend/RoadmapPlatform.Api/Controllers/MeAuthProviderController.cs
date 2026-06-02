using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public MeAuthProviderController(IAuthProviderService authProviderService)
        {
            _authProviderService = authProviderService;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAuthProviders()
        {
            var userId = GetCurrentUserId();

            var response = await _authProviderService.GetAuthProviderStatusAsync(userId);

            return Ok(response);
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

            return Redirect("http://localhost:5173/home");
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

            return Redirect("http://localhost:5173/home");
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
    }
}