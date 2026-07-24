using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.Roadmaps;
using RoadmapPlatform.Application.Interfaces.Roadmaps;

namespace RoadmapPlatform.Api.Controllers.Roadmaps;

/// <summary>
/// Provides learner-facing access to published roadmaps and their graph structure.
/// </summary>
[ApiController]
[Route("api/roadmaps")]
public sealed class RoadmapsController(IRoadmapQueryService roadmapQueryService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionConstant.ROADMAP_VIEW_PUBLISHED)]
    [ProducesResponseType(typeof(IReadOnlyList<RoadmapSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoadmaps(CancellationToken cancellationToken)
    {
        var result = await roadmapQueryService.GetPublishedRoadmapsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{slug}")]
    [RequirePermission(PermissionConstant.ROADMAP_VIEW_PUBLISHED)]
    [ProducesResponseType(typeof(RoadmapDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoadmapBySlug(
        string slug,
        CancellationToken cancellationToken)
    {
        var userId = TryGetCurrentUserId();
        var result = await roadmapQueryService.GetPublishedRoadmapBySlugAsync(
            slug,
            userId,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{slug}/graph")]
    [RequirePermission(PermissionConstant.ROADMAP_VIEW_PUBLISHED)]
    [ProducesResponseType(typeof(RoadmapGraphDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoadmapGraphBySlug(
        string slug,
        CancellationToken cancellationToken)
    {
        var userId = TryGetCurrentUserId();
        var result = await roadmapQueryService.GetPublishedRoadmapGraphBySlugAsync(
            slug,
            userId,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{roadmapVersionId:guid}/nodes/{roadmapNodeId:guid}")]
    [RequirePermission(PermissionConstant.ROADMAP_NODE_VIEW_PUBLISHED)]
    [ProducesResponseType(typeof(RoadmapNodeDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoadmapNodeDetail(
        Guid roadmapVersionId,
        Guid roadmapNodeId,
        CancellationToken cancellationToken)
    {
        var userId = TryGetCurrentUserId();
        var result = await roadmapQueryService.GetRoadmapNodeDetailAsync(
            roadmapVersionId,
            roadmapNodeId,
            userId,
            cancellationToken);

        return Ok(result);
    }

    private Guid? TryGetCurrentUserId()
    {
        var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(rawUserId, out var userId)
            ? userId
            : null;
    }
}
