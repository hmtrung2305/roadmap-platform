using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Application.Interfaces.Streaks;
using System.Security.Claims;

namespace RoadmapPlatform.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/streak")]
    public class StreakController : ControllerBase
    {
        private readonly IStreakService _streakService;

        public StreakController(IStreakService streakService)
        {
            _streakService = streakService;
        }
        [HttpGet]
        public async Task<IActionResult> GetStreak()
        {
            var userId = GetCurrentUserId();

            var result = await _streakService.GetStreakAsync(userId);

            return Ok(result);
        }

        [HttpPost("track")]
        public async Task<IActionResult> TrackStreak()
        {
            var userId = GetCurrentUserId();

            var result = await _streakService.TrackStreakAsync(userId);

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
}
