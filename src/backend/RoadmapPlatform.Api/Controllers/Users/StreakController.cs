using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.Interfaces.Streaks;

namespace RoadmapPlatform.Api.Controllers.Users
{
    /// <summary>
    /// Provides endpoints for viewing and tracking the current user's learning streak.
    /// </summary>
    [ApiController]
    [Route("api/streak")]
    public class StreakController : ControllerBase
    {
        private readonly IStreakService _streakService;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreakController"/> class.
        /// </summary>
        /// <param name="streakService">The streak service used to read and update user streak data.</param>
        public StreakController(IStreakService streakService)
        {
            _streakService = streakService;
        }

        /// <summary>
        /// Gets the current authenticated user's streak information.
        /// </summary>
        [HttpGet]
        [RequirePermission(PermissionConstant.STREAK_VIEW_SELF)]
        public async Task<IActionResult> GetStreak()
        {
            var userId = GetCurrentUserId();

            var result = await _streakService.GetStreakAsync(userId);

            return Ok(result);
        }

        /// <summary>
        /// Tracks the current authenticated user's activity and updates their streak.
        /// </summary>
        [HttpPost("track")]
        [RequirePermission(PermissionConstant.STREAK_TRACK_SELF)]
        public async Task<IActionResult> TrackStreak()
        {
            var userId = GetCurrentUserId();

            var result = await _streakService.TrackStreakAsync(userId);

            return Ok(result);
        }

        /// <summary>
        /// Gets the current authenticated user's identifier from claims.
        /// </summary>
        /// <returns>The current user identifier.</returns>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown when the current principal does not contain a valid user id claim.
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
}