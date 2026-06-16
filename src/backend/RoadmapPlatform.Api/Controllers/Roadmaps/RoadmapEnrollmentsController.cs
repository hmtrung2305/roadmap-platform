using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Api.Responses;
using RoadmapPlatform.Application.DTOs.Roadmaps;
using RoadmapPlatform.Application.Interfaces.Roadmaps;

namespace RoadmapPlatform.Api.Controllers.Roadmaps;

[ApiController]
[Authorize]
[Route("api/roadmap-enrollments")]
public sealed class RoadmapEnrollmentsController(
    IRoadmapEnrollmentService roadmapEnrollmentService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(RoadmapEnrollmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Enroll(
        [FromBody] EnrollRoadmapRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await roadmapEnrollmentService.EnrollAsync(
            userId,
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("current")]
    [ProducesResponseType(typeof(RoadmapEnrollmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentEnrollment(
        [FromQuery] Guid roadmapVersionId,
        CancellationToken cancellationToken)
    {
        if (roadmapVersionId == Guid.Empty)
        {
            return BadRequest(ApiErrorResponseFactory.Create(
                HttpContext,
                StatusCodes.Status400BadRequest,
                "INVALID_REQUEST",
                "Roadmap version id is required."));
        }

        var userId = GetCurrentUserId();
        var result = await roadmapEnrollmentService.GetCurrentEnrollmentAsync(
            userId,
            roadmapVersionId,
            cancellationToken);

        return result == null ? NoContent() : Ok(result);
    }

    private Guid GetCurrentUserId()
    {
        var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(rawUserId, out var userId))
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        return userId;
    }
}
