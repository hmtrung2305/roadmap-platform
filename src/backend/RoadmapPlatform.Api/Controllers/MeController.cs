using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Application.DTOs.Users;
using RoadmapPlatform.Application.Interfaces;

namespace RoadmapPlatform.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/me")]
public class MeController : ControllerBase
{
    private readonly IUserService _userService;

    public MeController(IUserService userService)
    {
        _userService = userService;
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