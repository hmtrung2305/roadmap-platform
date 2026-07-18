using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.SkillGapAnalysis.History;
using RoadmapPlatform.Application.Interfaces.SkillGapAnalysis;
using System.Security.Claims;

namespace RoadmapPlatform.Api.Controllers.SkillGapAnalysis
{
    [ApiController]
    [Route("api/")]
    public class SkillGapHistoryController : ControllerBase
    {
        private readonly ISkillGapHistoryService _skillGapHistoryService;

        public SkillGapHistoryController(
            ISkillGapHistoryService skillGapHistoryService)
        {
            _skillGapHistoryService = skillGapHistoryService;
        }

        [Authorize]
        [RequirePermission(
            PermissionConstant.SKILL_GAP_ANALYSIS_HISTORY_VIEW_SELF)]
        [HttpGet("me/skill-gap/history")]
        public async Task<IActionResult> GetHistory(
            [FromQuery] SkillGapHistoryPageRequestDto request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            var result = await _skillGapHistoryService.GetHistoryAsync(
                userId,
                request,
                cancellationToken);

            return Ok(result);
        }

        [Authorize]
        [RequirePermission(
            PermissionConstant.SKILL_GAP_ANALYSIS_HISTORY_VIEW_SELF)]
        [HttpGet("me/skill-gap/history/{historyId:guid}")]
        public async Task<IActionResult> GetHistoryDetail(
            Guid historyId,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            var result =
                await _skillGapHistoryService.GetHistoryDetailAsync(
                    userId,
                    historyId,
                    cancellationToken);

            return Ok(result);
        }

        [Authorize]
        [RequirePermission(
            PermissionConstant.SKILL_GAP_ANALYSIS_HISTORY_DELETE_SELF)]
        [HttpDelete("me/skill-gap/history/{historyId:guid}")]
        public async Task<IActionResult> DeleteHistory(
            Guid historyId,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            await _skillGapHistoryService.DeleteHistoryAsync(
                userId,
                historyId,
                cancellationToken);

            return NoContent();
        }

        private Guid GetCurrentUserId()
        {
            return Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }
    }
}