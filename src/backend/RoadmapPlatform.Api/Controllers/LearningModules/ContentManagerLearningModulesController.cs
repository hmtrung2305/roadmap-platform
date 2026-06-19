using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Api.Constants;
using RoadmapPlatform.Api.Extensions;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.LearningModules;
using RoadmapPlatform.Application.Interfaces.LearningModules;

namespace RoadmapPlatform.Api.Controllers.LearningModules;

[ApiController]
[Route("api/content/learning-modules")]
public sealed class ContentManagerLearningModulesController(
    IContentManagerLearningModuleService moduleService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionConstant.LEARNING_MODULE_VIEW_OWN)]
    [ProducesResponseType(typeof(ContentManagerLearningModuleListResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetModules(
        [FromQuery] ContentManagerLearningModuleListQueryDto query,
        CancellationToken cancellationToken)
    {
        var contentManagerUserId = User.GetUserId();

        var result = await moduleService.GetModulesAsync(
            contentManagerUserId,
            query,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionConstant.LEARNING_MODULE_CREATE_OWN)]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(SkillModuleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateModule(
        [FromBody] CreateLearningModuleRequestDto request,
        CancellationToken cancellationToken)
    {
        var contentManagerUserId = User.GetUserId();

        var result = await moduleService.CreateModuleAsync(
            contentManagerUserId,
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetModuleDetail),
            new { moduleId = result.SkillModuleId },
            result);
    }

    [HttpGet("{moduleId:guid}")]
    [RequirePermission(PermissionConstant.LEARNING_MODULE_VIEW_OWN)]
    [ProducesResponseType(typeof(ContentManagerLearningModuleDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetModuleDetail(
        Guid moduleId,
        CancellationToken cancellationToken)
    {
        var contentManagerUserId = User.GetUserId();

        var result = await moduleService.GetModuleDetailAsync(
            contentManagerUserId,
            moduleId,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{moduleId:guid}/overview")]
    [RequirePermission(PermissionConstant.LEARNING_MODULE_VIEW_OWN)]
    [ProducesResponseType(typeof(SkillModuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetModuleOverview(
        Guid moduleId,
        CancellationToken cancellationToken)
    {
        var contentManagerUserId = User.GetUserId();

        var result = await moduleService.GetModuleOverviewAsync(
            contentManagerUserId,
            moduleId,
            cancellationToken);

        return Ok(result);
    }

    [HttpPatch("{moduleId:guid}")]
    [RequirePermission(PermissionConstant.LEARNING_MODULE_UPDATE_OWN)]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(SkillModuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateModule(
        Guid moduleId,
        [FromBody] UpdateLearningModuleRequestDto request,
        CancellationToken cancellationToken)
    {
        var contentManagerUserId = User.GetUserId();

        var result = await moduleService.UpdateModuleAsync(
            contentManagerUserId,
            moduleId,
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{moduleId:guid}")]
    [RequirePermission(PermissionConstant.LEARNING_MODULE_DELETE_OWN)]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteDraftModule(
        Guid moduleId,
        CancellationToken cancellationToken)
    {
        var contentManagerUserId = User.GetUserId();

        await moduleService.DeleteDraftModuleAsync(
            contentManagerUserId,
            moduleId,
            cancellationToken);

        return NoContent();
    }

    [HttpGet("{moduleId:guid}/publish-readiness")]
    [RequirePermission(PermissionConstant.LEARNING_MODULE_VIEW_OWN)]
    [ProducesResponseType(typeof(PublishLearningModuleReadinessDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublishReadiness(
        Guid moduleId,
        CancellationToken cancellationToken)
    {
        var contentManagerUserId = User.GetUserId();

        var result = await moduleService.GetPublishReadinessAsync(
            contentManagerUserId,
            moduleId,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{moduleId:guid}/publish")]
    [RequirePermission(PermissionConstant.LEARNING_MODULE_PUBLISH_OWN)]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(PublishLearningModuleResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PublishModule(
        Guid moduleId,
        CancellationToken cancellationToken)
    {
        var contentManagerUserId = User.GetUserId();

        var result = await moduleService.PublishModuleAsync(
            contentManagerUserId,
            moduleId,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{moduleId:guid}/archive")]
    [RequirePermission(PermissionConstant.LEARNING_MODULE_ARCHIVE_OWN)]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(SkillModuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ArchiveModule(
        Guid moduleId,
        CancellationToken cancellationToken)
    {
        var contentManagerUserId = User.GetUserId();

        var result = await moduleService.ArchiveModuleAsync(
            contentManagerUserId,
            moduleId,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{moduleId:guid}/preview")]
    [RequirePermission(PermissionConstant.LEARNING_MODULE_PREVIEW_OWN)]
    [ProducesResponseType(typeof(LearningModulePreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPreview(
        Guid moduleId,
        CancellationToken cancellationToken)
    {
        var contentManagerUserId = User.GetUserId();

        var result = await moduleService.GetPreviewAsync(
            contentManagerUserId,
            moduleId,
            cancellationToken);

        return Ok(result);
    }
}
