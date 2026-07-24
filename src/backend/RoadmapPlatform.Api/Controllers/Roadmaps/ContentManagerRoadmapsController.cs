using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Api.Constants;
using RoadmapPlatform.Api.Extensions;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.Roadmaps.ContentManagement;
using RoadmapPlatform.Application.Interfaces.Roadmaps.ContentManagement;

namespace RoadmapPlatform.Api.Controllers.Roadmaps;

/// <summary>
/// Manages roadmap drafts owned by content managers.
/// </summary>
[ApiController]
[Route("api/content/roadmaps")]
public sealed class ContentManagerRoadmapsController(
    IContentManagerRoadmapService roadmapService,
    IAuthorizationService authorizationService) : ControllerBase
{
    [HttpGet]
    [RequireAnyPermission(PermissionConstant.ROADMAP_DRAFT_VIEW_OWN, PermissionConstant.ROADMAP_REVIEW_VIEW_ANY)]
    [ProducesResponseType(typeof(ContentRoadmapListResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoadmaps(
        [FromQuery] ContentRoadmapListQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await roadmapService.GetRoadmapsAsync(
            query,
            User.GetUserId(),
            await CanViewAllRoadmapsAsync(),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionConstant.ROADMAP_DRAFT_CREATE_OWN)]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(ContentRoadmapDetailDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRoadmap(
        [FromBody] CreateRoadmapRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await roadmapService.CreateRoadmapAsync(
            request,
            User.GetUserId(),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetRoadmapDetail),
            new { roadmapId = result.RoadmapId, versionId = result.RoadmapVersionId },
            result);
    }

    [HttpGet("{roadmapId:guid}")]
    [RequireAnyPermission(PermissionConstant.ROADMAP_DRAFT_VIEW_OWN, PermissionConstant.ROADMAP_REVIEW_VIEW_ANY)]
    [ProducesResponseType(typeof(ContentRoadmapDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoadmapDetail(
        Guid roadmapId,
        [FromQuery] Guid? versionId,
        CancellationToken cancellationToken)
    {
        var result = await roadmapService.GetRoadmapDetailAsync(
            roadmapId,
            versionId,
            User.GetUserId(),
            await CanViewAllRoadmapsAsync(),
            cancellationToken);

        return Ok(result);
    }

    private async Task<bool> CanViewAllRoadmapsAsync()
    {
        var result = await authorizationService.AuthorizeAsync(
            User,
            PermissionPolicyNames.For(PermissionConstant.ROADMAP_REVIEW_VIEW_ANY));

        return result.Succeeded;
    }
}
