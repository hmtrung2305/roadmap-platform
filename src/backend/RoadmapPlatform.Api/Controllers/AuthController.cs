using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Application.DTOs.Auth;
using RoadmapPlatform.Application.Interfaces.Auth;

namespace RoadmapPlatform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private const string ExternalScheme = "External";
        private const string AccessTokenCookieName = "access_token";

        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequestDto request)
        {
            var response = await _authService.RegisterAsync(request);

            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto request)
        {
            var loginResponse = await _authService.LoginAsync(request);

            AppendAccessTokenCookie(loginResponse.AccessToken);

            return Ok(new
            {
                user = loginResponse.User,
                message = "Logged in successfully"
            });
        }

        [HttpPost("registration/verify-email")]
        public async Task<IActionResult> VerifyRegistrationEmail(
            VerifyRegistrationEmailRequestDto request)
        {
            var loginResponse = await _authService.VerifyRegistrationEmailAsync(request);

            AppendAccessTokenCookie(loginResponse.AccessToken);

            return Ok(new
            {
                user = loginResponse.User,
                message = "Email verified successfully"
            });
        }

        [HttpPost("registration/resend-verification")]
        public async Task<IActionResult> ResendRegistrationVerification(
            ResendRegistrationVerificationRequestDto request)
        {
            await _authService.ResendRegistrationVerificationAsync(request);

            return Ok(new
            {
                message = "Verification code sent"
            });
        }

        [HttpGet("google/login")]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(GoogleCallback))
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google/callback")]
        public async Task<IActionResult> GoogleCallback()
        {
            var result = await HttpContext.AuthenticateAsync(ExternalScheme);

            if (!result.Succeeded || result.Principal == null)
            {
                return Unauthorized("Google authentication failed");
            }

            try
            {
                var loginResponse = await _authService.LoginWithGoogleAsync(result.Principal);

                await HttpContext.SignOutAsync(ExternalScheme);

                AppendAccessTokenCookie(loginResponse.AccessToken);

                return Redirect("http://localhost:5173/home");
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
                return Unauthorized("GitHub authentication failed");
            }

            try
            {
                var loginResponse = await _authService.LoginWithGitHubAsync(result.Principal);

                await HttpContext.SignOutAsync(ExternalScheme);

                AppendAccessTokenCookie(loginResponse.AccessToken);

                return Redirect("http://localhost:5173/home");
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

        private void AppendAccessTokenCookie(string accessToken)
        {
            Response.Cookies.Append(AccessTokenCookieName, accessToken, new CookieOptions
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