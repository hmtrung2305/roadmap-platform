using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Api.Constants;
using RoadmapPlatform.Api.Extensions;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.LearningModules;
using RoadmapPlatform.Application.Interfaces.LearningModules;

namespace RoadmapPlatform.Api.Controllers.LearningModules;

/// <summary>
/// Manages quiz configuration and questions for learning-module authors.
/// </summary>
[ApiController]
[EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
[Route("api/content/learning-modules/{moduleId:guid}/quiz")]
public sealed class ContentManagerLearningModuleQuizController(
    ILearningModuleQuizService quizService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionConstant.LEARNING_MODULE_QUIZ_VIEW_OWN)]
    [ProducesResponseType(typeof(LearningModuleQuizDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuiz(
        Guid moduleId,
        CancellationToken cancellationToken)
    {
        var contentManagerUserId = User.GetUserId();

        var result = await quizService.GetQuizAsync(
            contentManagerUserId,
            moduleId,
            cancellationToken);

        return Ok(result);
    }

    [HttpPut]
    [RequirePermission(PermissionConstant.LEARNING_MODULE_QUIZ_UPSERT_OWN)]
    [ProducesResponseType(typeof(LearningModuleQuizDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpsertQuiz(
        Guid moduleId,
        [FromBody] UpsertQuizRequestDto request,
        CancellationToken cancellationToken)
    {
        var contentManagerUserId = User.GetUserId();

        var result = await quizService.UpsertQuizAsync(
            contentManagerUserId,
            moduleId,
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("questions")]
    [RequirePermission(PermissionConstant.LEARNING_MODULE_QUIZ_QUESTION_CREATE_OWN)]
    [ProducesResponseType(typeof(LearningModuleQuizQuestionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddQuestion(
        Guid moduleId,
        [FromBody] UpsertQuizQuestionRequestDto request,
        CancellationToken cancellationToken)
    {
        var contentManagerUserId = User.GetUserId();

        var result = await quizService.AddQuestionAsync(
            contentManagerUserId,
            moduleId,
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(AddQuestion),
            new { moduleId },
            result);
    }

    [HttpPatch("questions/{questionId:guid}")]
    [RequirePermission(PermissionConstant.LEARNING_MODULE_QUIZ_QUESTION_UPDATE_OWN)]
    [ProducesResponseType(typeof(LearningModuleQuizQuestionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateQuestion(
        Guid moduleId,
        Guid questionId,
        [FromBody] UpsertQuizQuestionRequestDto request,
        CancellationToken cancellationToken)
    {
        var contentManagerUserId = User.GetUserId();

        var result = await quizService.UpdateQuestionAsync(
            contentManagerUserId,
            moduleId,
            questionId,
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpPatch("questions/reorder")]
    [RequirePermission(PermissionConstant.LEARNING_MODULE_QUIZ_QUESTION_REORDER_OWN)]
    [ProducesResponseType(typeof(IReadOnlyList<LearningModuleQuizQuestionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReorderQuestions(
        Guid moduleId,
        [FromBody] ReorderQuizQuestionsRequestDto request,
        CancellationToken cancellationToken)
    {
        var contentManagerUserId = User.GetUserId();

        var result = await quizService.ReorderQuestionsAsync(
            contentManagerUserId,
            moduleId,
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("questions/{questionId:guid}")]
    [RequirePermission(PermissionConstant.LEARNING_MODULE_QUIZ_QUESTION_DELETE_OWN)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteQuestion(
        Guid moduleId,
        Guid questionId,
        CancellationToken cancellationToken)
    {
        var contentManagerUserId = User.GetUserId();

        await quizService.DeleteQuestionAsync(
            contentManagerUserId,
            moduleId,
            questionId,
            cancellationToken);

        return NoContent();
    }
}
