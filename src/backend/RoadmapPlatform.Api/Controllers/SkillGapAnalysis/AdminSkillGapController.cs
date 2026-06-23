using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.SkillGapAnalysis;
using RoadmapPlatform.Application.Interfaces.CareerRoleSkill;

namespace RoadmapPlatform.Api.Controllers.SkillGapAnalysis
{
    [ApiController]
    [Route("api/content/skill-gap")]
    public class AdminSkillGapController : ControllerBase
    {

        private readonly ISkillGapAnalysisService _skillGapAnalysisService;
        public AdminSkillGapController(ISkillGapAnalysisService skillGapAnalysisService)
        {
            _skillGapAnalysisService = skillGapAnalysisService;
        }

        // CONTENT MANAGER
        [HttpGet("{careerRoleSlug}/levels")]
        [RequirePermission(PermissionConstant.SKILL_GAP_CONFIG_VIEW_ANY)]
        public async Task<IActionResult> GetAssessmentLevelsAdmin(string careerRoleSlug)
        {
            var result =
                await _skillGapAnalysisService
                    .GetAssessmentLevelsContentManagerAsync(careerRoleSlug);

            return Ok(result);
        }


        [HttpGet("{careerRoleSlug}/levels/{levelSlug}/groups")]
        [RequirePermission(PermissionConstant.SKILL_GAP_CONFIG_VIEW_ANY)]
        public async Task<IActionResult> GetGroupsByLevel(string careerRoleSlug, string levelSlug)
        {
            var result =
                await _skillGapAnalysisService
                    .GetAssessmentGroupsByLevelContentManagerAsync(
                        careerRoleSlug,
                        levelSlug);

            return Ok(result);
        }


        [HttpPut("{careerRoleSlug}/levels/{levelSlug}/groups")]
        [RequirePermission(PermissionConstant.SKILL_GAP_CONFIG_UPDATE_ANY)]
        public async Task<IActionResult> UpdateAssessmentGroupsByLevel(string careerRoleSlug, string levelSlug, [FromBody] UpdateAssessmentLevelGroupsDto request)
        {
            await _skillGapAnalysisService
                .UpdateAssessmentLevelGroupsAsync(
                    careerRoleSlug,
                    levelSlug,
                    request);

            return NoContent();
        }



    }
}
