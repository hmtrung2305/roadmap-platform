using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Api.Extensions;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.ContentWorkspace;
using RoadmapPlatform.Application.Interfaces.LearningModules;

namespace RoadmapPlatform.Api.Controllers.ContentWorkspace;

[ApiController]
[Route("api/content/workspace")]
public sealed class ContentWorkspaceController(
    IContentManagerLearningModuleService moduleService) : ControllerBase
{
    [HttpGet("overview")]
    [RequirePermission(PermissionConstant.LEARNING_MODULE_VIEW_OWN)]
    [ProducesResponseType(typeof(ContentWorkspaceOverviewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var contentManagerUserId = User.GetUserId();

        var result = await moduleService.GetWorkspaceOverviewAsync(
            contentManagerUserId,
            cancellationToken);

        return Ok(result);
    }
}
