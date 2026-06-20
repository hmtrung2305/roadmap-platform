using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.SkillGapAnalysis;
using RoadmapPlatform.Application.Interfaces.CareerRoleSkill;

namespace RoadmapPlatform.Api.Controllers.SkillGapAnalysis
{
    [ApiController]
    [Route("api/admin/skill-gap")]
    public class AdminSkillGapController : ControllerBase
    {

        private readonly ISkillGapAnalysisService _skillGapAnalysisService;
        public AdminSkillGapController(ISkillGapAnalysisService skillGapAnalysisService)
        {
            _skillGapAnalysisService = skillGapAnalysisService;
        }

        // ADMIN
        [RequirePermission(PermissionConstant.SKILL_GAP_CONFIG_VIEW_ANY)]
        [HttpGet("assessment-groups/{careerRoleSlug}")]
        public async Task<IActionResult> GetAssessmentGroups(string careerRoleSlug)
        {
            var result =
                await _skillGapAnalysisService
                    .GetAssessmentGroupsAsync(
                        careerRoleSlug);

            return Ok(result);
        }



        [RequirePermission(PermissionConstant.SKILL_GAP_CONFIG_UPDATE_ANY)]
        [HttpPut("assessment-groups")]
        public async Task<IActionResult> UpdateAssessmentGroups([FromBody] List<UpdateAssessmentGroupDto> request)
        {
            await _skillGapAnalysisService
                .UpdateAssessmentGroupsAsync(
                    request);

            return NoContent();
        }



    }
}
