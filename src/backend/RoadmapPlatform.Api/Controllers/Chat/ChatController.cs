using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Api.Extensions;
using RoadmapPlatform.Application.DTOs.AiCredits;
using RoadmapPlatform.Application.DTOs.Chat;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.AiCredits;
using RoadmapPlatform.Application.Interfaces.Chat;

namespace RoadmapPlatform.Api.Controllers.Chat
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IAiCreditService _aiCreditService;
        private readonly IChatService _chatService;

        public ChatController(IAiCreditService aiCreditService, IChatService chatService)
        {
            _aiCreditService = aiCreditService;
            _chatService = chatService;
        }

        [HttpGet("credits")]
        public async Task<ActionResult<AiCreditStatusDto>> GetCredits()
        {
            var userId = User.GetUserId();
            var credits = await _aiCreditService.GetStatusAsync(userId);

            return Ok(credits);
        }

        [HttpPost]
        public async Task<ActionResult<ChatResponseDto>> Chat([FromBody] ChatRequestDto request)
        {
            try
            {
                var userId = User.GetUserId();
                var response = await _chatService.ChatAsync(userId, request);

                return Ok(response);
            }
            catch (AiCreditLimitExceededException ex)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new
                {
                    message = ex.Message,
                    credits = ex.Status
                });
            }
        }
    }
}