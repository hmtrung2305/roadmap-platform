using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.Interfaces.SkillGapAnalysis;

namespace RoadmapPlatform.Api.Controllers.SkillGapAnalysis
{
    /// <summary>
    /// Manages learner skill-gap assessment answers and submission.
    /// </summary>
    [ApiController]
    [Route("api/")]
    public class SkillGapAssessmentController : ControllerBase
    {
        private readonly ISkillGapAssessmentService _skillGapAssessmentService;

        public SkillGapAssessmentController(
            ISkillGapAssessmentService skillGapAssessmentService)
        {
            _skillGapAssessmentService = skillGapAssessmentService;
        }

        [Authorize]
        [RequirePermission(PermissionConstant.CAREER_ROLE_VIEW_CATALOG)]
        [HttpGet("skill-gap/roadmaps/{roadmapId:guid}/assessment")]
        public async Task<IActionResult> GetAssessment(Guid roadmapId)
        {
            var result = await _skillGapAssessmentService
                .GetAssessmentAsync(roadmapId);

            return Ok(result);
        }
    }
}
