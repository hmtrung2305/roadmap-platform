using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Api.Extensions;
using RoadmapPlatform.Application.Interfaces.AiCredits;

namespace RoadmapPlatform.Api.Controllers.AiCredits
{
    [ApiController]
    [Authorize]
    [Route("api/ai-credits")]
    public class AiCreditsController : ControllerBase
    {
        private readonly IAiCreditService _aiCreditService;

        public AiCreditsController(IAiCreditService aiCreditService)
        {
            _aiCreditService = aiCreditService;
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var status = await _aiCreditService.GetStatusAsync(userId, cancellationToken);

            return Ok(status);
        }
    }
}
