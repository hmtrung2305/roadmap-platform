using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Api.Extensions;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.LearningModules;
using RoadmapPlatform.Application.Interfaces.LearningModules;

namespace RoadmapPlatform.Api.Controllers.LearningModules;

[ApiController]
[Route("api/learning-modules")]
public sealed class LearningModulesController(
    ILearnerLearningModuleService moduleService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionConstant.LEARNING_MODULE_VIEW_PUBLISHED)]
    [ProducesResponseType(typeof(IReadOnlyList<LearnerLearningModuleSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublishedModules(CancellationToken cancellationToken)
    {
        var result = await moduleService.GetPublishedModulesAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("enrolled")]
    [RequirePermission(PermissionConstant.LEARNING_MODULE_ENROLLMENT_VIEW_SELF)]
    [ProducesResponseType(typeof(IReadOnlyList<LearnerLearningModuleSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEnrolledModules(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var result = await moduleService.GetEnrolledModulesAsync(
            userId,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{slug}")]
    [RequirePermission(PermissionConstant.LEARNING_MODULE_VIEW_PUBLISHED)]
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
    [RequirePermission(PermissionConstant.LEARNING_MODULE_ENROLLMENT_CREATE_SELF)]
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
    [RequirePermission(PermissionConstant.LEARNING_MODULE_LESSON_VIEW_ENROLLED)]
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
    [RequirePermission(PermissionConstant.LEARNING_MODULE_PROGRESS_UPDATE_SELF)]
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

    [HttpGet("{moduleId:guid}/quiz/attempts")]
    [RequirePermission(PermissionConstant.LEARNING_MODULE_QUIZ_ATTEMPT_VIEW_SELF)]
    [ProducesResponseType(typeof(IReadOnlyList<QuizAttemptSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetQuizAttempts(
        Guid moduleId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var result = await moduleService.GetQuizAttemptsAsync(
            userId,
            moduleId,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{moduleId:guid}/quiz/attempts")]
    [RequirePermission(PermissionConstant.LEARNING_MODULE_QUIZ_ATTEMPT_CREATE_SELF)]
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

    [HttpGet("{moduleId:guid}/quiz/attempts/{attemptId:guid}/session")]
    [RequirePermission(PermissionConstant.LEARNING_MODULE_QUIZ_ATTEMPT_VIEW_SELF)]
    [ProducesResponseType(typeof(StartQuizAttemptResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetQuizAttemptSession(
        Guid moduleId,
        Guid attemptId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var result = await moduleService.GetQuizAttemptSessionAsync(
            userId,
            moduleId,
            attemptId,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{moduleId:guid}/quiz/attempts/{attemptId:guid}/submit")]
    [RequirePermission(PermissionConstant.LEARNING_MODULE_QUIZ_ATTEMPT_SUBMIT_SELF)]
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
    [RequirePermission(PermissionConstant.LEARNING_MODULE_QUIZ_ATTEMPT_VIEW_SELF)]
    [ProducesResponseType(typeof(QuizAttemptReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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
