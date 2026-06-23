using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RoadmapPlatform.Api.Constants;
using RoadmapPlatform.Application.DTOs.ContentRoadmaps;
using RoadmapPlatform.Application.Interfaces.ContentRoadmaps;

namespace RoadmapPlatform.Api.Controllers.ContentRoadmaps;

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

[ApiController]
[Route("api/content/roadmap-versions")]
public sealed class ContentManagerRoadmapVersionsController(
    IContentManagerRoadmapService roadmapService) : ControllerBase
{
    [HttpPatch("{roadmapVersionId:guid}/metadata")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(ContentRoadmapDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRoadmapVersionMetadata(
        Guid roadmapVersionId,
        [FromBody] UpdateRoadmapVersionMetadataRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await roadmapService.UpdateRoadmapVersionMetadataAsync(
            roadmapVersionId,
            request,
            cancellationToken);

        return Ok(result);
    }
}

[ApiController]
[Route("api/content/roadmap-nodes")]
public sealed class ContentManagerRoadmapNodesController(
    IContentManagerRoadmapService roadmapService) : ControllerBase
{
    [HttpPatch("{roadmapNodeId:guid}/metadata")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(ContentRoadmapNodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRoadmapNodeMetadata(
        Guid roadmapNodeId,
        [FromBody] UpdateRoadmapNodeMetadataRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await roadmapService.UpdateRoadmapNodeMetadataAsync(
            roadmapNodeId,
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{roadmapNodeId:guid}/resources")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(ContentRoadmapNodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddResourceToNode(
        Guid roadmapNodeId,
        [FromBody] AddRoadmapNodeResourceRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await roadmapService.AddResourceToNodeAsync(
            roadmapNodeId,
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{roadmapNodeId:guid}/resources/{learningResourceId:guid}")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(ContentRoadmapNodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveResourceFromNode(
        Guid roadmapNodeId,
        Guid learningResourceId,
        CancellationToken cancellationToken)
    {
        var result = await roadmapService.RemoveResourceFromNodeAsync(
            roadmapNodeId,
            learningResourceId,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{roadmapNodeId:guid}/skills")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(ContentRoadmapNodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddSkillToNode(
        Guid roadmapNodeId,
        [FromBody] AddRoadmapNodeSkillRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await roadmapService.AddSkillToNodeAsync(
            roadmapNodeId,
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{roadmapNodeId:guid}/skills/{skillId:guid}")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(ContentRoadmapNodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveSkillFromNode(
        Guid roadmapNodeId,
        Guid skillId,
        CancellationToken cancellationToken)
    {
        var result = await roadmapService.RemoveSkillFromNodeAsync(
            roadmapNodeId,
            skillId,
            cancellationToken);

        return Ok(result);
    }
}

[ApiController]
[Route("api/content/learning-resources")]
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
