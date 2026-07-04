using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.SkillGapAnalysis.CategoryConfig;
using RoadmapPlatform.Application.Interfaces.SkillGapAnalysis;
using System.Security.Claims;

namespace RoadmapPlatform.Api.Controllers.SkillGapAnalysis
{
    [ApiController]
    [Route("api/")]
    public class SkillGapConfigurationController : ControllerBase
    {
        private readonly ISkillGapCategoryConfigService _skillGapCategoryConfigService;

        public SkillGapConfigurationController(ISkillGapCategoryConfigService skillGapCategoryConfigService)
        {
            _skillGapCategoryConfigService = skillGapCategoryConfigService;
        }


        [Authorize]
        [RequirePermission(PermissionConstant.SKILL_GAP_CONFIG_VIEW_ANY)]
        [HttpGet("content/roadmaps/{roadmapId:guid}/categories")]
        public async Task<IActionResult> GetCategoryConfiguration(Guid roadmapId)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var result = await _skillGapCategoryConfigService
                .GetCategoryConfigurationAsync(userId, roadmapId);

            return Ok(result);
        }

        [Authorize]
        [RequirePermission(PermissionConstant.SKILL_GAP_CONFIG_UPDATE_ANY)]
        [HttpPut("content/roadmaps/{roadmapId:guid}/categories")]
        public async Task<IActionResult> UpdateCategoryConfiguration(Guid roadmapId, [FromBody] List<UpdateCategoryDisplayOrderDto> request)
        {

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            await _skillGapCategoryConfigService
                .UpdateCategoryDisplayOrderAsync(
                    userId,
                    roadmapId,
                    request);

            return NoContent();
        }

        [Authorize]
        [RequirePermission(PermissionConstant.SKILL_GAP_CONFIG_VIEW_ANY)]
        [HttpGet("content/published-roadmaps")]
        public async Task<IActionResult> GetMyPublishedRoadmaps()
        {
            var userId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var result = await _skillGapCategoryConfigService
                .GetMyPublishedRoadmapsAsync(userId);

            return Ok(result);
        }
    }
}
