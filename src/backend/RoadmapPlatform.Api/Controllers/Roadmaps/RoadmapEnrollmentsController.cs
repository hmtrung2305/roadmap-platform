using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Api.Responses;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.Roadmaps;
using RoadmapPlatform.Application.Interfaces.Roadmaps;

namespace RoadmapPlatform.Api.Controllers.Roadmaps;

[ApiController]
[Route("api/roadmap-enrollments")]
public sealed class RoadmapEnrollmentsController(
    IRoadmapEnrollmentService roadmapEnrollmentService) : ControllerBase
{
    [HttpPost]
    [RequirePermission(PermissionConstant.ROADMAP_ENROLLMENT_CREATE_SELF)]
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
    [RequirePermission(PermissionConstant.ROADMAP_ENROLLMENT_VIEW_SELF)]
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

    [HttpPost("{roadmapEnrollmentId:guid}/migrate")]
    [RequirePermission(PermissionConstant.ROADMAP_ENROLLMENT_MIGRATE_SELF)]
    [ProducesResponseType(typeof(RoadmapEnrollmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MigrateEnrollment(
        Guid roadmapEnrollmentId,
        [FromBody] MigrateRoadmapEnrollmentRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await roadmapEnrollmentService.MigrateEnrollmentAsync(
            userId,
            roadmapEnrollmentId,
            request,
            cancellationToken);

        return Ok(result);
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
