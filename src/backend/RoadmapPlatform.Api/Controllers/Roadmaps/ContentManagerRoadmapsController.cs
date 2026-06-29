using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RoadmapPlatform.Api.Constants;
using RoadmapPlatform.Application.DTOs.Roadmaps.ContentManagement;
using RoadmapPlatform.Application.Interfaces.Roadmaps.ContentManagement;

namespace RoadmapPlatform.Api.Controllers.Roadmaps;

[ApiController]
[Route("api/content/roadmaps")]
public sealed class ContentManagerRoadmapsController(
    IContentManagerRoadmapService roadmapService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ContentRoadmapListResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoadmaps(
        [FromQuery] ContentRoadmapListQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await roadmapService.GetRoadmapsAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(ContentRoadmapDetailDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRoadmap(
        [FromBody] CreateRoadmapRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await roadmapService.CreateRoadmapAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetRoadmapDetail),
            new { roadmapId = result.RoadmapId, versionId = result.RoadmapVersionId },
            result);
    }

    [HttpGet("{roadmapId:guid}")]
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
            cancellationToken);

        return Ok(result);
    }
}

