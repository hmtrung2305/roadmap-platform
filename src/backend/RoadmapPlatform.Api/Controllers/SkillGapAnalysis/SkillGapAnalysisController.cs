using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.SkillGapAnalysis.Analysis;
using RoadmapPlatform.Application.Interfaces.SkillGapAnalysis;

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

        [Authorize]
        [RequirePermission(PermissionConstant.SKILL_GAP_ANALYSIS_CREATE_SELF)]
        [HttpPost("me/skill-gap/analyze")]
        public async Task<IActionResult> Analyze(AnalyzeSkillGapRequestDto request, CancellationToken cancellationToken)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var result = await _skillGapAnalysisService
                .AnalyzeAsync(userId, request, cancellationToken);

            return Ok(result);
        }
    }
}
