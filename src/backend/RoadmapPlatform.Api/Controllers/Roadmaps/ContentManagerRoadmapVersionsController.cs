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
[Route("api/content/roadmap-versions")]
public sealed class ContentManagerRoadmapVersionsController(
    IContentManagerRoadmapService roadmapService) : ControllerBase
{

    [HttpPost("{roadmapVersionId:guid}/clone-draft")]
    [RequirePermission(PermissionConstant.ROADMAP_DRAFT_CREATE_ANY)]
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
    [RequirePermission(PermissionConstant.ROADMAP_DRAFT_CREATE_ANY)]
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


    [HttpPost("{roadmapVersionId:guid}/minor-draft")]
    [RequirePermission(PermissionConstant.ROADMAP_DRAFT_CREATE_ANY)]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(ContentRoadmapDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateMinorRoadmapVersionDraft(
        Guid roadmapVersionId,
        [FromBody] CloneRoadmapVersionDraftRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await roadmapService.CreateMinorRoadmapVersionDraftAsync(
            roadmapVersionId,
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{roadmapVersionId:guid}/validate")]
    [RequireAnyPermission(PermissionConstant.ROADMAP_DRAFT_UPDATE_ANY, PermissionConstant.ROADMAP_REVIEW_VIEW_ANY)]
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
    [RequirePermission(PermissionConstant.ROADMAP_REVIEW_APPROVE_ANY)]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(ContentRoadmapDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PublishRoadmapVersion(
        Guid roadmapVersionId,
        CancellationToken cancellationToken)
    {
        var result = await roadmapService.ApproveRoadmapVersionAsync(
            roadmapVersionId,
            User.GetUserId(),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{roadmapVersionId:guid}/submit-review")]
    [RequirePermission(PermissionConstant.ROADMAP_REVIEW_SUBMIT_OWN)]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(ContentRoadmapDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitRoadmapVersionForReview(
        Guid roadmapVersionId,
        [FromBody] SubmitRoadmapVersionReviewRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await roadmapService.SubmitRoadmapVersionForReviewAsync(
            roadmapVersionId,
            User.GetUserId(),
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{roadmapVersionId:guid}/approve")]
    [RequirePermission(PermissionConstant.ROADMAP_REVIEW_APPROVE_ANY)]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(ContentRoadmapDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveRoadmapVersion(
        Guid roadmapVersionId,
        CancellationToken cancellationToken)
    {
        var result = await roadmapService.ApproveRoadmapVersionAsync(
            roadmapVersionId,
            User.GetUserId(),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{roadmapVersionId:guid}/reject")]
    [RequirePermission(PermissionConstant.ROADMAP_REVIEW_REJECT_ANY)]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(ContentRoadmapDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectRoadmapVersion(
        Guid roadmapVersionId,
        [FromBody] RejectRoadmapVersionReviewRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await roadmapService.RejectRoadmapVersionAsync(
            roadmapVersionId,
            User.GetUserId(),
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{roadmapVersionId:guid}")]
    [RequirePermission(PermissionConstant.ROADMAP_DRAFT_DELETE_ANY)]
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
    [RequirePermission(PermissionConstant.ROADMAP_DRAFT_UPDATE_ANY)]
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
    [RequirePermission(PermissionConstant.ROADMAP_DRAFT_UPDATE_ANY)]
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
