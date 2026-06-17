using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.Skills;
using RoadmapPlatform.Application.Interfaces.Skills;

namespace RoadmapPlatform.Api.Controllers.Skills;

[ApiController]
[Route("api/skills")]
public sealed class SkillsController(ISkillLookupService skillLookupService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionConstant.SKILL_VIEW_CATALOG)]
    [ProducesResponseType(typeof(SkillSearchResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchSkills(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] int? limit,
        [FromQuery] int? offset,
        CancellationToken cancellationToken)
    {
        var result = await skillLookupService.SearchSkillsAsync(
            search,
            category,
            limit,
            offset,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("categories")]
    [RequirePermission(PermissionConstant.SKILL_VIEW_CATALOG)]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var result = await skillLookupService.GetCategoriesAsync(cancellationToken);

        return Ok(result);
    }
}
