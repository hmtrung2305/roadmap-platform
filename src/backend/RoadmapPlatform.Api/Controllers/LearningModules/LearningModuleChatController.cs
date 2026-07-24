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
/// Answers learner questions using the context of an enrolled learning module.
/// </summary>
[ApiController]
[RequirePermission(PermissionConstant.LEARNING_MODULE_CHAT_USE_ENROLLED)]
[EnableRateLimiting(RateLimitPolicyNames.AiExpensive)]
[Route("api/learning-modules/{moduleId:guid}/assistant/chat")]
public sealed class LearningModuleChatController(
    ILearningModuleChatService chatService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(LearningModuleChatResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Ask(
        Guid moduleId,
        [FromBody] LearningModuleChatRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var result = await chatService.AskAsync(
            userId,
            moduleId,
            request,
            cancellationToken);

        return Ok(result);
    }
}
