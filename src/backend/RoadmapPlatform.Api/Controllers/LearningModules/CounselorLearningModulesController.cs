using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RoadmapPlatform.Api.Constants;
using RoadmapPlatform.Api.Extensions;
using RoadmapPlatform.Application.DTOs.LearningModules;
using RoadmapPlatform.Application.Interfaces.LearningModules;

namespace RoadmapPlatform.Api.Controllers.LearningModules;

[ApiController]
[Authorize]
[Route("api/counselor/learning-modules")]
public sealed class CounselorLearningModulesController(
    ICounselorLearningModuleService moduleService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CounselorLearningModuleSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetModules(
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var counselorUserId = User.GetUserId();

        var result = await moduleService.GetModulesAsync(
            counselorUserId,
            status,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(SkillModuleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateModule(
        [FromBody] CreateLearningModuleRequestDto request,
        CancellationToken cancellationToken)
    {
        var counselorUserId = User.GetUserId();

        var result = await moduleService.CreateModuleAsync(
            counselorUserId,
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetModuleDetail),
            new { moduleId = result.SkillModuleId },
            result);
    }

    [HttpGet("{moduleId:guid}")]
    [ProducesResponseType(typeof(CounselorLearningModuleDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetModuleDetail(
        Guid moduleId,
        CancellationToken cancellationToken)
    {
        var counselorUserId = User.GetUserId();

        var result = await moduleService.GetModuleDetailAsync(
            counselorUserId,
            moduleId,
            cancellationToken);

        return Ok(result);
    }

    [HttpPatch("{moduleId:guid}")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(SkillModuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateModule(
        Guid moduleId,
        [FromBody] UpdateLearningModuleRequestDto request,
        CancellationToken cancellationToken)
    {
        var counselorUserId = User.GetUserId();

        var result = await moduleService.UpdateModuleAsync(
            counselorUserId,
            moduleId,
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{moduleId:guid}")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteDraftModule(
        Guid moduleId,
        CancellationToken cancellationToken)
    {
        var counselorUserId = User.GetUserId();

        await moduleService.DeleteDraftModuleAsync(
            counselorUserId,
            moduleId,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{moduleId:guid}/publish")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(PublishLearningModuleResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PublishModule(
        Guid moduleId,
        CancellationToken cancellationToken)
    {
        var counselorUserId = User.GetUserId();

        var result = await moduleService.PublishModuleAsync(
            counselorUserId,
            moduleId,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{moduleId:guid}/archive")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(SkillModuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ArchiveModule(
        Guid moduleId,
        CancellationToken cancellationToken)
    {
        var counselorUserId = User.GetUserId();

        var result = await moduleService.ArchiveModuleAsync(
            counselorUserId,
            moduleId,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{moduleId:guid}/preview")]
    [ProducesResponseType(typeof(LearningModulePreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPreview(
        Guid moduleId,
        CancellationToken cancellationToken)
    {
        var counselorUserId = User.GetUserId();

        var result = await moduleService.GetPreviewAsync(
            counselorUserId,
            moduleId,
            cancellationToken);

        return Ok(result);
    }
}
