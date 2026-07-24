using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Api.Extensions;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.Interfaces.AiCredits;

namespace RoadmapPlatform.Api.Controllers.AiCredits
{
    /// <summary>
    /// Exposes the authenticated user's AI-credit balance and usage status.
    /// </summary>
    [ApiController]
    [Route("api/ai-credits")]
    public class AiCreditsController : ControllerBase
    {
        private readonly IAiCreditService _aiCreditService;

        public AiCreditsController(IAiCreditService aiCreditService)
        {
            _aiCreditService = aiCreditService;
        }

        [HttpGet("status")]
        [RequirePermission(PermissionConstant.AI_CREDIT_VIEW_SELF)]
        public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var status = await _aiCreditService.GetStatusAsync(userId, cancellationToken);

            return Ok(status);
        }
    }
}
