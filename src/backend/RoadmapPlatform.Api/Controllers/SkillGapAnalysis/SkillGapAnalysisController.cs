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


        [HttpGet("skill-gap/career-roles")]
        [RequirePermission(PermissionConstant.CAREER_ROLE_VIEW_CATALOG)]
        public async Task<IActionResult> GetCareerRoles()
        {
            var result =
                await _skillGapAnalysisService
                    .GetCareerRolesAsync();

            return Ok(result);
        }


        [HttpPost("skill-gap/analyze")]
        [RequirePermission(PermissionConstant.SKILL_GAP_ANALYSIS_CREATE_SELF)]
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
        


        [HttpGet("skill-gap/{careerRoleSlug}/levels")]
        [RequirePermission(PermissionConstant.SKILL_GAP_ANALYSIS_CREATE_SELF)]
        public async Task<IActionResult> GetAssessmentLevels(string careerRoleSlug)
        {
            var result =
                await _skillGapAnalysisService
                    .GetAssessmentLevelsAsync(
                        careerRoleSlug);

            return Ok(result);
        }


        [HttpGet("skill-gap/{careerRoleSlug}/assessment/{levelSlug}")]
        [RequirePermission(PermissionConstant.SKILL_GAP_ANALYSIS_CREATE_SELF)]
        public async Task<IActionResult> GetAssessmentByLevel(string careerRoleSlug, string levelSlug)
        {
            var result =
                await _skillGapAnalysisService
                    .GetAssessmentByLevelAsync(
                        careerRoleSlug,
                        levelSlug);

            return Ok(result);
        }
    }
}
