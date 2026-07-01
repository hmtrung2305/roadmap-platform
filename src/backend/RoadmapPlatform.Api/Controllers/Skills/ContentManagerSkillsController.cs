using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Api.Constants;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.Skills;
using RoadmapPlatform.Application.Interfaces.Skills;

namespace RoadmapPlatform.Api.Controllers.Skills;

[ApiController]
[Route("api/content/skills")]
public sealed class ContentManagerSkillsController(
    IContentSkillCatalogService skillCatalogService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionConstant.SKILL_VIEW_CATALOG)]
    [ProducesResponseType(typeof(ContentSkillSearchResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchSkills(
        [FromQuery] ContentSkillSearchQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await skillCatalogService.SearchSkillsAsync(query, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{skillId:guid}")]
    [RequirePermission(PermissionConstant.SKILL_VIEW_CATALOG)]
    [ProducesResponseType(typeof(ContentSkillDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSkill(
        Guid skillId,
        CancellationToken cancellationToken)
    {
        var result = await skillCatalogService.GetSkillAsync(skillId, cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionConstant.SKILL_CREATE_CATALOG)]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(ContentSkillDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateSkill(
        [FromBody] CreateContentSkillRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await skillCatalogService.CreateSkillAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetSkill), new { skillId = result.SkillId }, result);
    }

    [HttpPatch("{skillId:guid}")]
    [RequirePermission(PermissionConstant.SKILL_UPDATE_CATALOG)]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(ContentSkillDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateSkill(
        Guid skillId,
        [FromBody] UpdateContentSkillRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await skillCatalogService.UpdateSkillAsync(skillId, request, cancellationToken);

        return Ok(result);
    }
}
