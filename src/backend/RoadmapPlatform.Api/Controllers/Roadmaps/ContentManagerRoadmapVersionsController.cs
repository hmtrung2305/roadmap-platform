using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RoadmapPlatform.Api.Constants;
using RoadmapPlatform.Application.DTOs.Roadmaps.ContentManagement;
using RoadmapPlatform.Application.Interfaces.Roadmaps.ContentManagement;

namespace RoadmapPlatform.Api.Controllers.Roadmaps;

[ApiController]
[Route("api/content/roadmap-versions")]
public sealed class ContentManagerRoadmapVersionsController(
    IContentManagerRoadmapService roadmapService) : ControllerBase
{

    [HttpPost("{roadmapVersionId:guid}/clone-draft")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(ContentRoadmapDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CloneRoadmapVersionToDraft(
        Guid roadmapVersionId,
        [FromBody] CloneRoadmapVersionDraftRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await roadmapService.CloneRoadmapVersionToDraftAsync(
            roadmapVersionId,
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{roadmapVersionId:guid}/patch-draft")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(ContentRoadmapDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreatePatchRoadmapVersionDraft(
        Guid roadmapVersionId,
        [FromBody] CloneRoadmapVersionDraftRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await roadmapService.CreatePatchRoadmapVersionDraftAsync(
            roadmapVersionId,
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{roadmapVersionId:guid}/validate")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(ContentRoadmapValidationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ValidateRoadmapVersion(
        Guid roadmapVersionId,
        CancellationToken cancellationToken)
    {
        var result = await roadmapService.ValidateRoadmapVersionAsync(
            roadmapVersionId,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{roadmapVersionId:guid}/publish")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(ContentRoadmapDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PublishRoadmapVersion(
        Guid roadmapVersionId,
        CancellationToken cancellationToken)
    {
        var result = await roadmapService.PublishRoadmapVersionAsync(
            roadmapVersionId,
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{roadmapVersionId:guid}")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDraftRoadmapVersion(
        Guid roadmapVersionId,
        CancellationToken cancellationToken)
    {
        await roadmapService.DeleteDraftVersionAsync(
            roadmapVersionId,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{roadmapVersionId:guid}/nodes")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(ContentRoadmapStructureMutationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateRoadmapNode(
        Guid roadmapVersionId,
        [FromBody] CreateRoadmapNodeRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await roadmapService.CreateNodeAsync(
            roadmapVersionId,
            request,
            cancellationToken);

        return Ok(result);
    }
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

