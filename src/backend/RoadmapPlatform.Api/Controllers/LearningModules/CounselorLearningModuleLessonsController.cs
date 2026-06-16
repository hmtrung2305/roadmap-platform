using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RoadmapPlatform.Api.Constants;
using RoadmapPlatform.Api.Extensions;
using RoadmapPlatform.Application.DTOs.LearningModules;
using RoadmapPlatform.Application.Interfaces.LearningModules;
using System.Text.Json;

namespace RoadmapPlatform.Api.Controllers.LearningModules;

[ApiController]
[Authorize]
[Route("api/counselor/learning-modules/{moduleId:guid}/lessons")]
public sealed class CounselorLearningModuleLessonsController(
    ILearningModuleLessonService lessonService) : ControllerBase
{
    [HttpPost("bulk")]
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
        var counselorUserId = User.GetUserId();

        var request = JsonSerializer.Deserialize<BulkUploadLessonsRequestDto>(
            lessonsJson,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (request == null)
        {
            return BadRequest("Lesson metadata is required.");
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
            counselorUserId,
            moduleId,
            request,
            uploadedFiles,
            cancellationToken);

        return Ok(result);
    }

    [HttpPatch("reorder")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(IReadOnlyList<LearningModuleLessonDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReorderLessons(
        Guid moduleId,
        [FromBody] ReorderLessonsRequestDto request,
        CancellationToken cancellationToken)
    {
        var counselorUserId = User.GetUserId();

        var result = await lessonService.ReorderLessonsAsync(
            counselorUserId,
            moduleId,
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpPatch("{lessonId:guid}")]
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
        var counselorUserId = User.GetUserId();

        var result = await lessonService.UpdateLessonAsync(
            counselorUserId,
            moduleId,
            lessonId,
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpPut("{lessonId:guid}/content")]
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
            return BadRequest("Markdown file cannot be empty.");
        }

        var counselorUserId = User.GetUserId();

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
            counselorUserId,
            moduleId,
            lessonId,
            uploadedFile,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{lessonId:guid}/reindex")]
    [EnableRateLimiting(RateLimitPolicyNames.AiExpensive)]
    [ProducesResponseType(typeof(LearningModuleLessonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReindexLesson(
        Guid moduleId,
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        var counselorUserId = User.GetUserId();

        var result = await lessonService.ReindexLessonAsync(
            counselorUserId,
            moduleId,
            lessonId,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{lessonId:guid}/preview")]
    [ProducesResponseType(typeof(LearningModuleLessonContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLessonPreview(
        Guid moduleId,
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        var counselorUserId = User.GetUserId();

        var result = await lessonService.GetLessonPreviewAsync(
            counselorUserId,
            moduleId,
            lessonId,
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{lessonId:guid}")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteDraftLesson(
        Guid moduleId,
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        var counselorUserId = User.GetUserId();

        await lessonService.DeleteDraftLessonAsync(
            counselorUserId,
            moduleId,
            lessonId,
            cancellationToken);

        return NoContent();
    }
}
