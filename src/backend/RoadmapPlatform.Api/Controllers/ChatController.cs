using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Application.DTOs.Chat;
using RoadmapPlatform.Application.Interfaces.Chat;

namespace RoadmapPlatform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost]
        public async Task<ActionResult<ChatResponseDto>> Chat([FromBody] ChatRequestDto request)
        {
            var response = await _chatService.ChatAsync(request);

            return Ok(response);
        }
    }
}