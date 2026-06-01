using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Application.DTOs.Auth;
using RoadmapPlatform.Application.Interfaces;
using System.Security.Claims;

namespace RoadmapPlatform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private const string ExternalScheme = "External";

        private readonly IAuthService _authService;

        public AuthController(IAuthService authServices)
        {
            _authService = authServices;
        }

        [Authorize]
        [HttpGet("auth-test")]
        public IActionResult AuthTest()
        {
            return Ok(new
            {
                message = "Token is valid",
                userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                username = User.Identity?.Name
            });
        }

        [HttpGet("google/login")]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties
            {
                // This returns the endpoint of the google callback
                RedirectUri = Url.Action(nameof(GoogleCallback))
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google/callback")]
        public async Task<IActionResult> GoogleCallback()
        {
            // Read the external cookie that was created during Google login 
            var result = await HttpContext.AuthenticateAsync(ExternalScheme);

            if (!result.Succeeded || result.Principal == null)
            {
                return Unauthorized("Google authentication failed");
            }

            try
            {
                LoginResponseDto loginResponse = await _authService.LoginWithGoogleAsync(result.Principal);

                await HttpContext.SignOutAsync(ExternalScheme);

                AppendAccessTokenCookie(loginResponse.AccessToken);

                return Redirect($"http://localhost:5173/home");
            }
            catch (Exception ex)
            {
                await HttpContext.SignOutAsync(ExternalScheme);
                return RedirectWithOAuthError(ex.Message);
            }
        }

        [HttpGet("github/login")]
        public IActionResult GitHubLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(GitHubCallback))
            };

            return Challenge(properties, GitHubAuthenticationDefaults.AuthenticationScheme);
        }

        [HttpGet("github/callback")]
        public async Task<IActionResult> GitHubCallback()
        {
            var result = await HttpContext.AuthenticateAsync(ExternalScheme);

            if (!result.Succeeded || result.Principal == null)
            {
                return Unauthorized("GitHub authentication failed.");
            }
            try
            {
                var loginResponse = await _authService.LoginWithGitHubAsync(
                                result.Principal);

                await HttpContext.SignOutAsync(ExternalScheme);

                AppendAccessTokenCookie(loginResponse.AccessToken);

                return Redirect($"http://localhost:5173/home");
            }
            catch (Exception ex)
            {
                await HttpContext.SignOutAsync(ExternalScheme);
                return RedirectWithOAuthError(ex.Message);
            }

        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("access_token", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None
            });

            return Ok(new
            {
                message = "Logged out successfully"
            });
        }

        private void AppendAccessTokenCookie(string accessToken)
        {
            Response.Cookies.Append("access_token", accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddMinutes(60)
            });
        }

        private IActionResult RedirectWithOAuthError(string message)
        {
            var encodedMessage = Uri.EscapeDataString(message);

            return Redirect($"http://localhost:5173/login?oauthError={encodedMessage}");
        }
    }
}
