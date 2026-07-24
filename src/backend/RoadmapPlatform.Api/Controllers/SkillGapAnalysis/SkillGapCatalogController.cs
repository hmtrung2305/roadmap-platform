using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.Interfaces.SkillGapAnalysis;

namespace RoadmapPlatform.Api.Controllers.SkillGapAnalysis
{
    /// <summary>
    /// Provides the catalogs required to complete a skill-gap assessment.
    /// </summary>
    [ApiController]
    [Route("api/")]
    public class SkillGapCatalogController : ControllerBase
    {
        private readonly ISkillGapCatalogService _skillGapCatalogService;

        public SkillGapCatalogController(ISkillGapCatalogService skillGapCatalogService)
        {
            _skillGapCatalogService = skillGapCatalogService;
        }

        [Authorize]
        [RequirePermission(PermissionConstant.CAREER_ROLE_VIEW_CATALOG)]
        [HttpGet("skill-gap/career-roles")]
        public async Task<IActionResult> GetCareerRoles()
        {
            var result =
                await _skillGapCatalogService
                    .GetCareerRolesAsync();

            return Ok(result);
        }

        [Authorize]
        [RequirePermission(PermissionConstant.CAREER_ROLE_VIEW_CATALOG)]
        [HttpGet("skill-gap/career-roles/{careerRoleSlug}/roadmaps")]
        public async Task<IActionResult> GetPublishedRoadmaps(
            string careerRoleSlug)
        {
            var result = await _skillGapCatalogService
                .GetPublishedRoadmapsAsync(careerRoleSlug);

            return Ok(result);
        }
    }
}
