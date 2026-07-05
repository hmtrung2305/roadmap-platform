using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.Interfaces.SkillGapAnalysis;
using System.Security.Claims;

namespace RoadmapPlatform.Api.Controllers.SkillGapAnalysis
{
    [ApiController]
    [Route("api/")]
    public class SkillGapHistoryController : ControllerBase
    {
        private readonly ISkillGapHistoryService _skillGapHistoryService;

        public SkillGapHistoryController(ISkillGapHistoryService skillGapHistoryService)
        {
            _skillGapHistoryService = skillGapHistoryService;
        }


        [Authorize]
        [RequirePermission(PermissionConstant.SKILL_GAP_ANALYSIS_HISTORY_VIEW_SELF)]
        [HttpGet("me/skill-gap/history")]
        public async Task<IActionResult> GetHistory()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var result = await _skillGapHistoryService
                .GetHistoryAsync(userId);

            return Ok(result);
        }



        [Authorize]
        [RequirePermission(PermissionConstant.SKILL_GAP_ANALYSIS_HISTORY_VIEW_SELF)]
        [HttpGet("me/skill-gap/history/{historyId:guid}")]
        public async Task<IActionResult> GetHistoryDetail(Guid historyId)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var result = await _skillGapHistoryService
                .GetHistoryDetailAsync(
                    userId,
                    historyId);

            return Ok(result);
        }


        [Authorize]
        [RequirePermission(PermissionConstant.SKILL_GAP_ANALYSIS_HISTORY_DELETE_SELF)]
        [HttpDelete("me/skill-gap/history/{historyId:guid}")]
        public async Task<IActionResult> DeleteHistory(Guid historyId)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            await _skillGapHistoryService
                .DeleteHistoryAsync(
                    userId,
                    historyId);

            return NoContent();
        }
    }
}
