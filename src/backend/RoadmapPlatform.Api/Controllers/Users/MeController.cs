using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.Portfolio;
using RoadmapPlatform.Application.DTOs.Users;
using RoadmapPlatform.Application.Interfaces.Portfolio;
using RoadmapPlatform.Application.Interfaces.Users;
using System.Security.Claims;

namespace RoadmapPlatform.Api.Controllers.Users;

[ApiController]
[Route("api/me")]
public class MeController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IPortfolioService _portfolioService;
    private readonly IAccountProfileService _accountProfileService;

    public MeController(
        IUserService userService,
        IPortfolioService portfolioService,
        IAccountProfileService accountProfileService)
    {
        _userService = userService;
        _portfolioService = portfolioService;
        _accountProfileService = accountProfileService;
    }

    [HttpGet]
    [RequirePermission(PermissionConstant.ACCOUNT_VIEW_SELF)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = GetCurrentUserId();
        var result = await _userService.GetCurrentUserAsync(userId);

        return Ok(result);
    }

    [HttpPatch]
    [RequirePermission(PermissionConstant.ACCOUNT_UPDATE_SELF)]
    public async Task<IActionResult> UpdateCurrentUser(UpdateCurrentUserRequestDto request)
    {
        var userId = GetCurrentUserId();
        var result = await _userService.UpdateCurrentUserAsync(userId, request);

        return Ok(result);
    }

    [HttpDelete]
    [RequirePermission(PermissionConstant.ACCOUNT_DELETE_SELF)]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = GetCurrentUserId();

        await _userService.DeleteAccountAsync(userId);

        return NoContent();
    }


    [HttpGet("account-profile")]
    [RequirePermission(PermissionConstant.ACCOUNT_VIEW_SELF)]
    public async Task<IActionResult> GetAccountProfile()
    {
        var userId = GetCurrentUserId();
        var result = await _accountProfileService.GetAccountProfileAsync(userId);

        return Ok(result);
    }

    [HttpPatch("account-profile")]
    [RequirePermission(PermissionConstant.ACCOUNT_UPDATE_SELF)]
    public async Task<IActionResult> UpdateAccountProfile(UpdateAccountProfileRequestDto request)
    {
        var userId = GetCurrentUserId();
        var result = await _accountProfileService.UpdateAccountProfileAsync(userId, request);

        return Ok(result);
    }

    [HttpGet("profile")]
    [RequirePermission(PermissionConstant.PROFILE_VIEW_SELF)]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = GetCurrentUserId();
        var result = await _userService.GetMyProfileAsync(userId);

        return Ok(result);
    }

    [HttpPatch("profile")]
    [RequirePermission(PermissionConstant.PROFILE_UPDATE_SELF)]
    public async Task<IActionResult> UpdateMyProfile(UpdateProfileRequestDto request)
    {
        var userId = GetCurrentUserId();
        var result = await _userService.UpdateMyProfileAsync(userId, request);

        return Ok(result);
    }

    [HttpGet("portfolio")]
    [RequirePermission(PermissionConstant.PORTFOLIO_VIEW_SELF)]
    public async Task<IActionResult> GetMyPortfolio()
    {
        var userId = GetCurrentUserId();

        var portfolioResponse = await _portfolioService.GetMyPortfolioAsync(userId);

        return Ok(portfolioResponse);
    }

    [HttpPatch("portfolio/repositories")]
    [RequirePermission(PermissionConstant.PORTFOLIO_UPDATE_SELF)]
    public async Task<IActionResult> UpdatePortfolioRepositories(UpdatePortfolioRepositoriesRequestDto request)
    {
        var userId = GetCurrentUserId();

        var portfolioResponse = await _portfolioService.UpdatePortfolioRepositoriesAsync(userId, request);

        return Ok(portfolioResponse);
    }

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