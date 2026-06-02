using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Application.DTOs.Users;
using RoadmapPlatform.Application.Interfaces.Portfolio;
using RoadmapPlatform.Application.Interfaces.Users;
using RoadmapPlatform.Infrastructure.Services;
using System.Security.Claims;

namespace RoadmapPlatform.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/me")]
public class MeController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IPortfolioService _portfolioService;

    public MeController(IUserService userService, IPortfolioService portfolioService)
    {
        _userService = userService;
        _portfolioService = portfolioService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = GetCurrentUserId();
        var result = await _userService.GetCurrentUserAsync(userId);

        return Ok(result);
    }

    [HttpPatch]
    public async Task<IActionResult> UpdateCurrentUser(UpdateCurrentUserRequestDto request)
    {
        var userId = GetCurrentUserId();
        var result = await _userService.UpdateCurrentUserAsync(userId, request);

        return Ok(result);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = GetCurrentUserId();

        await _userService.DeleteAccountAsync(userId);

        return NoContent();
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = GetCurrentUserId();
        var result = await _userService.GetMyProfileAsync(userId);

        return Ok(result);
    }

    [HttpPatch("profile")]
    public async Task<IActionResult> UpdateMyProfile(UpdateProfileRequestDto request)
    {
        var userId = GetCurrentUserId();
        var result = await _userService.UpdateMyProfileAsync(userId, request);

        return Ok(result);
    }

    [HttpGet("portfolio")]
    [Authorize]
    public async Task<IActionResult> GetMyPortfolio()
    {
        var userId = GetCurrentUserId();

        var portfolioResponse = await _portfolioService.GetMyPortfolioAsync(userId);

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