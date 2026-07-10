using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.Portfolio;
using RoadmapPlatform.Application.DTOs.Users;
using RoadmapPlatform.Application.Interfaces.Portfolio;
using RoadmapPlatform.Application.Interfaces.Users;

namespace RoadmapPlatform.Api.Controllers.Users;

/// <summary>
/// Provides endpoints for the currently authenticated user's account, profile, and portfolio.
/// </summary>
[ApiController]
[Route("api/me")]
public class MeController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IPortfolioService _portfolioService;
    private readonly IAccountProfileService _accountProfileService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MeController"/> class.
    /// </summary>
    /// <param name="userService">The user service used to manage current user data.</param>
    /// <param name="portfolioService">The portfolio service used to manage the current user's portfolio.</param>
    /// <param name="accountProfileService">The account profile service used to manage account profile data.</param>
    public MeController(
        IUserService userService,
        IPortfolioService portfolioService,
        IAccountProfileService accountProfileService)
    {
        _userService = userService;
        _portfolioService = portfolioService;
        _accountProfileService = accountProfileService;
    }

    /// <summary>
    /// Gets the current authenticated user's account information.
    /// </summary>
    [HttpGet]
    [RequirePermission(PermissionConstant.ACCOUNT_VIEW_SELF)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = GetCurrentUserId();
        var result = await _userService.GetCurrentUserAsync(userId);

        return Ok(result);
    }

    /// <summary>
    /// Updates the current authenticated user's account information.
    /// </summary>
    /// <param name="request">The account update request.</param>
    [HttpPatch]
    [RequirePermission(PermissionConstant.ACCOUNT_UPDATE_SELF)]
    public async Task<IActionResult> UpdateCurrentUser(UpdateCurrentUserRequestDto request)
    {
        var userId = GetCurrentUserId();
        var result = await _userService.UpdateCurrentUserAsync(userId, request);

        return Ok(result);
    }

    /// <summary>
    /// Deletes or deactivates the current authenticated user's account.
    /// </summary>
    [HttpDelete]
    [RequirePermission(PermissionConstant.ACCOUNT_DELETE_SELF)]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = GetCurrentUserId();

        await _userService.DeleteAccountAsync(userId);

        return NoContent();
    }

    /// <summary>
    /// Gets the current authenticated user's account profile.
    /// </summary>
    [HttpGet("account-profile")]
    [RequirePermission(PermissionConstant.ACCOUNT_VIEW_SELF)]
    public async Task<IActionResult> GetAccountProfile()
    {
        var userId = GetCurrentUserId();
        var result = await _accountProfileService.GetAccountProfileAsync(userId);

        return Ok(result);
    }

    /// <summary>
    /// Updates the current authenticated user's account profile.
    /// </summary>
    /// <param name="request">The account profile update request.</param>
    [HttpPatch("account-profile")]
    [RequirePermission(PermissionConstant.ACCOUNT_UPDATE_SELF)]
    public async Task<IActionResult> UpdateAccountProfile(UpdateAccountProfileRequestDto request)
    {
        var userId = GetCurrentUserId();
        var result = await _accountProfileService.UpdateAccountProfileAsync(userId, request);

        return Ok(result);
    }

    /// <summary>
    /// Gets the current authenticated user's public learning profile.
    /// </summary>
    [HttpGet("profile")]
    [RequirePermission(PermissionConstant.PROFILE_VIEW_SELF)]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = GetCurrentUserId();
        var result = await _userService.GetMyProfileAsync(userId);

        return Ok(result);
    }

    /// <summary>
    /// Updates the current authenticated user's public learning profile.
    /// </summary>
    /// <param name="request">The profile update request.</param>
    [HttpPatch("profile")]
    [RequirePermission(PermissionConstant.PROFILE_UPDATE_SELF)]
    public async Task<IActionResult> UpdateMyProfile(UpdateProfileRequestDto request)
    {
        var userId = GetCurrentUserId();
        var result = await _userService.UpdateMyProfileAsync(userId, request);

        return Ok(result);
    }

    /// <summary>
    /// Gets the current authenticated user's portfolio.
    /// </summary>
    [HttpGet("portfolio")]
    [RequirePermission(PermissionConstant.PORTFOLIO_VIEW_SELF)]
    public async Task<IActionResult> GetMyPortfolio()
    {
        var userId = GetCurrentUserId();

        var portfolioResponse = await _portfolioService.GetMyPortfolioAsync(userId);

        return Ok(portfolioResponse);
    }

    /// <summary>
    /// Updates the repositories displayed in the current authenticated user's portfolio.
    /// </summary>
    /// <param name="request">The portfolio repository update request.</param>
    [HttpPatch("portfolio/repositories")]
    [RequirePermission(PermissionConstant.PORTFOLIO_UPDATE_SELF)]
    public async Task<IActionResult> UpdatePortfolioRepositories(UpdatePortfolioRepositoriesRequestDto request)
    {
        var userId = GetCurrentUserId();

        var portfolioResponse = await _portfolioService.UpdatePortfolioRepositoriesAsync(userId, request);

        return Ok(portfolioResponse);
    }

    /// <summary>
    /// Gets the current authenticated user's identifier from claims.
    /// </summary>
    /// <returns>The current user identifier.</returns>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the authenticated principal does not contain a valid user id claim.
    /// </exception>
    private Guid GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user id claim.");
        }

        return userId;
    }
}