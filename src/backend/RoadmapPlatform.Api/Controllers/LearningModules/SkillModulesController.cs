using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Api.Extensions;
using RoadmapPlatform.Application.DTOs.LearningModules;
using RoadmapPlatform.Application.Interfaces.LearningModules;

namespace RoadmapPlatform.Api.Controllers.LearningModules;

[ApiController]
[Route("api/skill-modules")]
public sealed class SkillModulesController(
    ILearnerLearningModuleService moduleService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<LearnerLearningModuleSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublishedModules(CancellationToken cancellationToken)
    {
        var userId = TryGetCurrentUserId();

        var result = await moduleService.GetPublishedModulesAsync(
            userId,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LearnerLearningModuleOverviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublishedModuleBySlug(
        string slug,
        CancellationToken cancellationToken)
    {
        var userId = TryGetCurrentUserId();

        var result = await moduleService.GetPublishedModuleBySlugAsync(
            slug,
            userId,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{moduleId:guid}/enroll")]
    [Authorize]
    [ProducesResponseType(typeof(LearningModuleEnrollmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Enroll(
        Guid moduleId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var result = await moduleService.EnrollAsync(
            userId,
            moduleId,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{moduleId:guid}/lessons/{lessonId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(LearningModuleLessonContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetLessonContent(
        Guid moduleId,
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var result = await moduleService.GetLessonContentAsync(
            userId,
            moduleId,
            lessonId,
            cancellationToken);

        return Ok(result);
    }

    [HttpPatch("{moduleId:guid}/lessons/{lessonId:guid}/progress")]
    [Authorize]
    [ProducesResponseType(typeof(UpdateLessonProgressResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateLessonProgress(
        Guid moduleId,
        Guid lessonId,
        [FromBody] UpdateLessonProgressRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var result = await moduleService.UpdateLessonProgressAsync(
            userId,
            moduleId,
            lessonId,
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{moduleId:guid}/quiz/attempts")]
    [Authorize]
    [ProducesResponseType(typeof(StartQuizAttemptResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> StartQuizAttempt(
        Guid moduleId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var result = await moduleService.StartQuizAttemptAsync(
            userId,
            moduleId,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{moduleId:guid}/quiz/attempts/{attemptId:guid}/submit")]
    [Authorize]
    [ProducesResponseType(typeof(QuizAttemptReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitQuizAttempt(
        Guid moduleId,
        Guid attemptId,
        [FromBody] SubmitQuizAttemptRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var result = await moduleService.SubmitQuizAttemptAsync(
            userId,
            moduleId,
            attemptId,
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{moduleId:guid}/quiz/attempts/{attemptId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(QuizAttemptReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuizAttemptReview(
        Guid moduleId,
        Guid attemptId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var result = await moduleService.GetQuizAttemptReviewAsync(
            userId,
            moduleId,
            attemptId,
            cancellationToken);

        return Ok(result);
    }

    private Guid? TryGetCurrentUserId()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return User.GetUserId();
    }
}
