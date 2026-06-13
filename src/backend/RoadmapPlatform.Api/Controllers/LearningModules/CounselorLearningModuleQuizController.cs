using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Api.Extensions;
using RoadmapPlatform.Application.DTOs.LearningModules;
using RoadmapPlatform.Application.Interfaces.LearningModules;

namespace RoadmapPlatform.Api.Controllers.LearningModules;

[ApiController]
[Authorize]
[Route("api/counselor/skill-modules/{moduleId:guid}/quiz")]
public sealed class CounselorLearningModuleQuizController(
    ILearningModuleQuizService quizService) : ControllerBase
{
    [HttpPut]
    [ProducesResponseType(typeof(LearningModuleQuizDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpsertQuiz(
        Guid moduleId,
        [FromBody] UpsertQuizRequestDto request,
        CancellationToken cancellationToken)
    {
        var counselorUserId = User.GetUserId();

        var result = await quizService.UpsertQuizAsync(
            counselorUserId,
            moduleId,
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("questions")]
    [ProducesResponseType(typeof(LearningModuleQuizQuestionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddQuestion(
        Guid moduleId,
        [FromBody] UpsertQuizQuestionRequestDto request,
        CancellationToken cancellationToken)
    {
        var counselorUserId = User.GetUserId();

        var result = await quizService.AddQuestionAsync(
            counselorUserId,
            moduleId,
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(AddQuestion),
            new { moduleId },
            result);
    }

    [HttpPatch("questions/{questionId:guid}")]
    [ProducesResponseType(typeof(LearningModuleQuizQuestionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateQuestion(
        Guid moduleId,
        Guid questionId,
        [FromBody] UpsertQuizQuestionRequestDto request,
        CancellationToken cancellationToken)
    {
        var counselorUserId = User.GetUserId();

        var result = await quizService.UpdateQuestionAsync(
            counselorUserId,
            moduleId,
            questionId,
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpPatch("questions/reorder")]
    [ProducesResponseType(typeof(IReadOnlyList<LearningModuleQuizQuestionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReorderQuestions(
        Guid moduleId,
        [FromBody] ReorderQuizQuestionsRequestDto request,
        CancellationToken cancellationToken)
    {
        var counselorUserId = User.GetUserId();

        var result = await quizService.ReorderQuestionsAsync(
            counselorUserId,
            moduleId,
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("questions/{questionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteQuestion(
        Guid moduleId,
        Guid questionId,
        CancellationToken cancellationToken)
    {
        var counselorUserId = User.GetUserId();

        await quizService.DeleteQuestionAsync(
            counselorUserId,
            moduleId,
            questionId,
            cancellationToken);

        return NoContent();
    }
}
