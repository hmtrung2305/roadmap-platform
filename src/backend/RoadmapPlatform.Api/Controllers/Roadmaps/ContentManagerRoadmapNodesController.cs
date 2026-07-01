using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Api.Constants;
using RoadmapPlatform.Api.Extensions;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.Roadmaps.ContentManagement;
using RoadmapPlatform.Application.Interfaces.Roadmaps.ContentManagement;

namespace RoadmapPlatform.Api.Controllers.Roadmaps;

[ApiController]
[Route("api/content/roadmap-nodes")]
[RequirePermission(PermissionConstant.ROADMAP_DRAFT_UPDATE_OWN)]
public sealed class ContentManagerRoadmapNodesController(
    IContentManagerRoadmapService roadmapService) : ControllerBase
{
    [HttpPost("{roadmapNodeId:guid}/move")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(ContentRoadmapStructureMutationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MoveRoadmapNode(
        Guid roadmapNodeId,
        [FromBody] MoveRoadmapNodeRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await roadmapService.MoveNodeAsync(
            roadmapNodeId,
            request,
            User.GetUserId(),
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{roadmapNodeId:guid}")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(ContentRoadmapStructureMutationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRoadmapNode(
        Guid roadmapNodeId,
        CancellationToken cancellationToken)
    {
        var result = await roadmapService.DeleteNodeAsync(
            roadmapNodeId,
            User.GetUserId(),
            cancellationToken);

        return Ok(result);
    }

    [HttpPatch("{roadmapNodeId:guid}/group-rule")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(ContentRoadmapStructureMutationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGroupRule(
        Guid roadmapNodeId,
        [FromBody] UpdateRoadmapNodeGroupRuleRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await roadmapService.UpdateGroupRuleAsync(
            roadmapNodeId,
            request,
            User.GetUserId(),
            cancellationToken);

        return Ok(result);
    }

    [HttpPatch("{roadmapNodeId:guid}/requirement")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(ContentRoadmapStructureMutationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateNodeRequirement(
        Guid roadmapNodeId,
        [FromBody] UpdateRoadmapNodeRequirementRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await roadmapService.UpdateNodeRequirementAsync(
            roadmapNodeId,
            request,
            User.GetUserId(),
            cancellationToken);

        return Ok(result);
    }

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
            User.GetUserId(),
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
            User.GetUserId(),
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
            User.GetUserId(),
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
            User.GetUserId(),
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
            User.GetUserId(),
            cancellationToken);

        return Ok(result);
    }
}
