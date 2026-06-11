using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> GetAllCareerRole()
        {
            var careerRole = await _skillGapAnalysisService.GetAllCareerRolesAsync();
            return Ok(careerRole);
        }

        [HttpGet("career-roles/{slug}")]
        public async Task<IActionResult> GetCareerRoleBySlug(string slug)
        {
            var careerRole = await _skillGapAnalysisService.GetCareerRoleBySlugAsync(slug);
            return Ok(careerRole);
        }

        [HttpGet("career-roles/{slug}/assessment-skills")]
        public async Task<IActionResult> GetAssessmentSkills(string slug)
        {
            var result = await _skillGapAnalysisService.GetAssessmentSkillBySlugAsync(slug);

            return Ok(result);
        }

        [HttpPost("career-roles/skill-gap/analyze")]
        public async Task<IActionResult> AnalyzeCareerRole(AnalyzeSkillGapRequestDto analyzeSkillGapRequest)
        {
            var result = await _skillGapAnalysisService.GetSkillGapResultAsync(analyzeSkillGapRequest);
            return Ok(result);
        }

        [HttpPost("career-roles/skill-gap/report")]
        public async Task<IActionResult> SkillGapReport(AnalyzeSkillGapRequestDto request)
        {
            var result = await _skillGapAnalysisService.GenerateSkillGapReportAsync(request);
            return Ok(result);
        }
    }
}
