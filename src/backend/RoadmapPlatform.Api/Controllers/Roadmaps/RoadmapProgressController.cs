using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.Roadmaps;
using RoadmapPlatform.Application.Interfaces.Roadmaps;

namespace RoadmapPlatform.Api.Controllers.Roadmaps;

/// <summary>
/// Updates learner progress for nodes in a roadmap enrollment.
/// </summary>
[ApiController]
[Route("api/roadmap-enrollments/{roadmapEnrollmentId:guid}/nodes")]
public sealed class RoadmapProgressController(
    IRoadmapProgressService roadmapProgressService) : ControllerBase
{
    [HttpPatch("{roadmapNodeId:guid}/progress")]
    [RequirePermission(PermissionConstant.ROADMAP_PROGRESS_UPDATE_SELF)]
    [ProducesResponseType(typeof(UpdateNodeProgressResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateNodeProgress(
        Guid roadmapEnrollmentId,
        Guid roadmapNodeId,
        [FromBody] UpdateNodeProgressRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await roadmapProgressService.UpdateNodeProgressAsync(
            userId,
            roadmapEnrollmentId,
            roadmapNodeId,
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
