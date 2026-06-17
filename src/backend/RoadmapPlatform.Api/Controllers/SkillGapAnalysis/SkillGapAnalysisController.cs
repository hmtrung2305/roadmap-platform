using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.SkillGapAnalysis;
using RoadmapPlatform.Application.Interfaces.CareerRoleSkill;

namespace RoadmapPlatform.Api.Controllers.SkillGap
{
    [ApiController]
    [Route("api/")]
    public class SkillGapAnalysisController : ControllerBase
    {
        private readonly ISkillGapAnalysisService _skillGapAnalysisService;

        public SkillGapAnalysisController(ISkillGapAnalysisService skillGapAnalysisService)
        {
            _skillGapAnalysisService = skillGapAnalysisService;
        }

        [HttpGet("career-roles")]
        [RequirePermission(PermissionConstant.CAREER_ROLE_VIEW_CATALOG)]
        public async Task<IActionResult> GetAllCareerRole()
        {
            var careerRole = await _skillGapAnalysisService.GetAllCareerRolesAsync();
            return Ok(careerRole);
        }

        [HttpGet("career-roles/{slug}")]
        [RequirePermission(PermissionConstant.CAREER_ROLE_VIEW_CATALOG)]
        public async Task<IActionResult> GetCareerRoleBySlug(string slug)
        {
            var careerRole = await _skillGapAnalysisService.GetCareerRoleBySlugAsync(slug);
            return Ok(careerRole);
        }

        [HttpGet("career-roles/{slug}/assessment-skills")]
        [RequirePermission(PermissionConstant.CAREER_ROLE_VIEW_CATALOG)]
        public async Task<IActionResult> GetAssessmentSkills(string slug)
        {
            var result = await _skillGapAnalysisService.GetAssessmentSkillBySlugAsync(slug);

            return Ok(result);
        }

        [HttpPost("career-roles/skill-gap/analyze")]
        [RequirePermission(PermissionConstant.SKILL_GAP_ANALYSIS_CREATE_SELF)]
        public async Task<IActionResult> AnalyzeCareerRole(AnalyzeSkillGapRequestDto analyzeSkillGapRequest)
        {
            var result = await _skillGapAnalysisService.GetSkillGapResultAsync(analyzeSkillGapRequest);
            return Ok(result);
        }
    }
}
