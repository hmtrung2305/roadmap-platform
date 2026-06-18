using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Api.Constants;
using RoadmapPlatform.Api.Extensions;
using RoadmapPlatform.Api.Responses;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.LearningModules;
using RoadmapPlatform.Application.Interfaces.LearningModules;
using System.Text.Json;

namespace RoadmapPlatform.Api.Controllers.LearningModules;

[ApiController]
[Route("api/content/learning-modules/{moduleId:guid}/lessons")]
public sealed class ContentManagerLearningModuleLessonsController(
    ILearningModuleLessonService lessonService) : ControllerBase
{
    [HttpPost("bulk")]
    [RequirePermission(PermissionConstant.LEARNING_MODULE_LESSON_CREATE_OWN)]
    [EnableRateLimiting(RateLimitPolicyNames.UploadExpensive)]
    [RequestSizeLimit(100_000_000)]
    [ProducesResponseType(typeof(BulkUploadLessonsResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> BulkUploadLessons(
        Guid moduleId,
        [FromForm] string lessonsJson,
        [FromForm] List<IFormFile> files,
        CancellationToken cancellationToken)
    {
        var contentManagerUserId = User.GetUserId();

        var request = JsonSerializer.Deserialize<BulkUploadLessonsRequestDto>(
            lessonsJson,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (request == null)
        {
            return BadRequest(ApiErrorResponseFactory.Create(
                HttpContext,
                StatusCodes.Status400BadRequest,
                "INVALID_REQUEST",
                "Lesson metadata is required."));
        }

        var uploadedFiles = files
            .Select(file => new LearningModuleUploadedFileDto
            {
                FileName = file.FileName,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                    ? "text/markdown"
                    : file.ContentType,
                Length = file.Length,
                Content = file.OpenReadStream()
            })
            .ToList();

        var result = await lessonService.BulkUploadLessonsAsync(
            contentManagerUserId,
            moduleId,
            request,
            uploadedFiles,
            cancellationToken);

        return Ok(result);
    }

    [HttpPatch("reorder")]
    [RequirePermission(PermissionConstant.LEARNING_MODULE_LESSON_REORDER_OWN)]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(IReadOnlyList<LearningModuleLessonDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReorderLessons(
        Guid moduleId,
        [FromBody] ReorderLessonsRequestDto request,
        CancellationToken cancellationToken)
    {
        var contentManagerUserId = User.GetUserId();

        var result = await lessonService.ReorderLessonsAsync(
            contentManagerUserId,
            moduleId,
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpPatch("{lessonId:guid}")]
    [RequirePermission(PermissionConstant.LEARNING_MODULE_LESSON_UPDATE_OWN)]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(LearningModuleLessonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateLessonMetadata(
        Guid moduleId,
        Guid lessonId,
        [FromBody] UpdateLearningModuleLessonRequestDto request,
        CancellationToken cancellationToken)
    {
        var contentManagerUserId = User.GetUserId();

        var result = await lessonService.UpdateLessonAsync(
            contentManagerUserId,
            moduleId,
            lessonId,
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpPut("{lessonId:guid}/content")]
    [RequirePermission(PermissionConstant.LEARNING_MODULE_LESSON_UPDATE_OWN)]
    [EnableRateLimiting(RateLimitPolicyNames.UploadExpensive)]
    [RequestSizeLimit(50_000_000)]
    [ProducesResponseType(typeof(LearningModuleLessonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReplaceLessonMarkdown(
        Guid moduleId,
        Guid lessonId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(ApiErrorResponseFactory.Create(
                HttpContext,
                StatusCodes.Status400BadRequest,
                "INVALID_REQUEST",
                "Markdown file cannot be empty."));
        }

        var contentManagerUserId = User.GetUserId();

        var uploadedFile = new LearningModuleUploadedFileDto
        {
            FileName = file.FileName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "text/markdown"
                : file.ContentType,
            Length = file.Length,
            Content = file.OpenReadStream()
        };

        var result = await lessonService.ReplaceLessonContentAsync(
            contentManagerUserId,
            moduleId,
            lessonId,
            uploadedFile,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{lessonId:guid}/reindex")]
    [RequirePermission(PermissionConstant.LEARNING_MODULE_LESSON_REINDEX_OWN)]
    [EnableRateLimiting(RateLimitPolicyNames.AiExpensive)]
    [ProducesResponseType(typeof(LearningModuleLessonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReindexLesson(
        Guid moduleId,
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        var contentManagerUserId = User.GetUserId();

        var result = await lessonService.ReindexLessonAsync(
            contentManagerUserId,
            moduleId,
            lessonId,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{lessonId:guid}/preview")]
    [RequirePermission(PermissionConstant.LEARNING_MODULE_PREVIEW_OWN)]
    [ProducesResponseType(typeof(LearningModuleLessonContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLessonPreview(
        Guid moduleId,
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        var contentManagerUserId = User.GetUserId();

        var result = await lessonService.GetLessonPreviewAsync(
            contentManagerUserId,
            moduleId,
            lessonId,
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{lessonId:guid}")]
    [RequirePermission(PermissionConstant.LEARNING_MODULE_LESSON_DELETE_OWN)]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteDraftLesson(
        Guid moduleId,
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        var contentManagerUserId = User.GetUserId();

        await lessonService.DeleteDraftLessonAsync(
            contentManagerUserId,
            moduleId,
            lessonId,
            cancellationToken);

        return NoContent();
    }
}
