using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Api.Constants;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.Roadmaps.ContentManagement;
using RoadmapPlatform.Application.Interfaces.Roadmaps.ContentManagement;

namespace RoadmapPlatform.Api.Controllers.Roadmaps;

[ApiController]
[Route("api/content/learning-resources")]
[RequirePermission(PermissionConstant.ROADMAP_DRAFT_UPDATE_OWN)]
public sealed class ContentManagerLearningResourcesController(
    IContentManagerRoadmapService roadmapService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ContentLearningResourceSearchResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchLearningResources(
        [FromQuery] string? search,
        [FromQuery] int limit,
        CancellationToken cancellationToken)
    {
        var result = await roadmapService.SearchLearningResourcesAsync(
            search,
            limit,
            cancellationToken);

        return Ok(result);
    }
}
