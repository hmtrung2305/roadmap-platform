using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Api.Constants;
using RoadmapPlatform.Api.Extensions;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.AiMentor;
using RoadmapPlatform.Application.Interfaces.AiMentor;

namespace RoadmapPlatform.Api.Controllers.AiMentor
{
    [ApiController]
    [Route("api/ai-mentor")]
    [RequirePermission(PermissionConstant.AI_MENTOR_CHAT_USE_SELF)]
    public sealed class AiMentorController(IAiMentorService aiMentorService) : ControllerBase
    {
        [HttpGet("conversations")]
        [ProducesResponseType(typeof(IReadOnlyList<AiMentorConversationDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetConversations(CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();

            var result = await aiMentorService.GetConversationsAsync(userId, cancellationToken);

            return Ok(result);
        }

        [HttpGet("conversations/{conversationId:guid}/messages")]
        [ProducesResponseType(typeof(IReadOnlyList<AiMentorMessageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMessages(Guid conversationId, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();

            var result = await aiMentorService.GetMessagesAsync(userId, conversationId, cancellationToken);

            return Ok(result);
        }

        [HttpPost("chat")]
        [EnableRateLimiting(RateLimitPolicyNames.AiExpensive)]
        [ProducesResponseType(typeof(AiMentorChatResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Chat(
        [FromBody] AiMentorChatRequestDto request,
        CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();

            var result = await aiMentorService.AskAsync(
                userId,
                request,
                cancellationToken);

            return Ok(result);
        }

        [HttpDelete("conversations/{conversationId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ArchiveConversation(
            Guid conversationId,
            CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();

            await aiMentorService.ArchiveConversationAsync(
                userId,
                conversationId,
                cancellationToken);

            return NoContent();
        }
    }
}
