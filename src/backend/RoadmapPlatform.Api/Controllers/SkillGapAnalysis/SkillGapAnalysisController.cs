using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.SkillGapAnalysis;
using RoadmapPlatform.Application.Interfaces.CareerRoleSkill;
using System.Security.Claims;

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








        [RequirePermission(PermissionConstant.CAREER_ROLE_VIEW_CATALOG)]
        [HttpGet("skill-gap/career-roles")]
        public async Task<IActionResult> GetCareerRoles()
        {
            var result =
                await _skillGapAnalysisService
                    .GetCareerRolesAsync();

            return Ok(result);
        }


        [RequirePermission(PermissionConstant.SKILL_VIEW_CATALOG)]
        [HttpGet("skill-gap/{careerRoleSlug}/assessment")]
        public async Task<IActionResult> GetAssessment(string careerRoleSlug)
        {
            var result =
                await _skillGapAnalysisService
                    .GetAssessmentAsync(careerRoleSlug);

            return Ok(result);
        }


        [RequirePermission(PermissionConstant.SKILL_GAP_ANALYSIS_CREATE_SELF)]
        [HttpPost("skill-gap/analyze")]
        public async Task<IActionResult> Analyze([FromBody] AnalyzeSkillGapRequestDto request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var result = await _skillGapAnalysisService.AnalyzeAsync(userId, request);

            return Ok(result);
        }




        [HttpGet("me/skill-gap/history")]
        [RequirePermission(PermissionConstant.SKILL_GAP_ANALYSIS_HISTORY_VIEW_SELF)]
        public async Task<IActionResult> GetHistory()
        {
            var userId = Guid.Parse(
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier)!);

            var result =
                await _skillGapAnalysisService
                    .GetHistoryAsync(userId);

            return Ok(result);
        }

        [HttpGet("me/skill-gap/history/{historyId:guid}")]
        [RequirePermission(PermissionConstant.SKILL_GAP_ANALYSIS_HISTORY_VIEW_SELF)]
        public async Task<IActionResult> GetHistoryDetail(Guid historyId)
        {
            var userId = Guid.Parse(
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier)!);

            var result =
                await _skillGapAnalysisService
                    .GetHistoryDetailAsync(
                        historyId,
                        userId);

            return Ok(result);
        }

        [HttpDelete("me/skill-gap/history/{historyId:guid}")]
        [RequirePermission(PermissionConstant.SKILL_GAP_ANALYSIS_HISTORY_DELETE_SELF)]
        public async Task<IActionResult> DeleteHistory(Guid historyId)
        {
            var userId = Guid.Parse(
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier)!);

            await _skillGapAnalysisService
                .DeleteHistoryAsync(
                    historyId,
                    userId);

            return NoContent();
        }
    }
}
