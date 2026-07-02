using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Api.Constants;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.LearningResources;
using RoadmapPlatform.Application.Interfaces.LearningResources;

namespace RoadmapPlatform.Api.Controllers.Roadmaps;

[ApiController]
[Route("api/content/learning-resources")]
public sealed class ContentManagerLearningResourcesController(
    IContentLearningResourceCatalogService learningResourceCatalogService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionConstant.LEARNING_RESOURCE_VIEW_CATALOG)]
    [ProducesResponseType(typeof(ContentLearningResourceSearchResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchLearningResources(
        [FromQuery] ContentLearningResourceSearchQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await learningResourceCatalogService.SearchLearningResourcesAsync(query, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{learningResourceId:guid}")]
    [RequirePermission(PermissionConstant.LEARNING_RESOURCE_VIEW_CATALOG)]
    [ProducesResponseType(typeof(ContentLearningResourceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLearningResource(
        Guid learningResourceId,
        CancellationToken cancellationToken)
    {
        var result = await learningResourceCatalogService.GetLearningResourceAsync(
            learningResourceId,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionConstant.LEARNING_RESOURCE_CREATE_CATALOG)]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(ContentLearningResourceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateLearningResource(
        [FromBody] CreateContentLearningResourceRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await learningResourceCatalogService.CreateLearningResourceAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetLearningResource),
            new { learningResourceId = result.ResourceId },
            result);
    }

    [HttpPatch("{learningResourceId:guid}")]
    [RequirePermission(PermissionConstant.LEARNING_RESOURCE_UPDATE_CATALOG)]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(ContentLearningResourceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateLearningResource(
        Guid learningResourceId,
        [FromBody] UpdateContentLearningResourceRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await learningResourceCatalogService.UpdateLearningResourceAsync(
            learningResourceId,
            request,
            cancellationToken);

        return Ok(result);
    }
}
