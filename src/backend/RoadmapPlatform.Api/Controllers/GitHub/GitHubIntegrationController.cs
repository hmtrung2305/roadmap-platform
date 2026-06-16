using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RoadmapPlatform.Api.Constants;
using RoadmapPlatform.Application.Interfaces.GitHub;
using System.Security.Claims;

namespace RoadmapPlatform.Api.Controllers.GitHub
{
    [Route("api/integrations/github")]
    [ApiController]
    public class GitHubIntegrationController : ControllerBase
    {
        private readonly IGitHubRepositoryService _gitHubRepositoryService;
        private readonly IRepoInsightService _repoInsightService;

        public GitHubIntegrationController(
            IGitHubRepositoryService gitHubRepositoryService,
            IRepoInsightService repoInsightService)
        {
            _gitHubRepositoryService = gitHubRepositoryService;
            _repoInsightService = repoInsightService;
        }

        [HttpGet("repositories")]
        [Authorize]
        public async Task<IActionResult> GetSavedRepositories()
        {
            var userId = GetCurrentUserId();

            var repositories = await _gitHubRepositoryService
                .GetSavedRepositoriesAsync(userId);

            return Ok(repositories);
        }

        [HttpPost("repositories/sync")]
        [Authorize]
        public async Task<IActionResult> SyncRepositories()
        {
            var userId = GetCurrentUserId();

            var repositories = await _gitHubRepositoryService
                .SyncPublicRepositoriesAsync(userId);

            return Ok(repositories);
        }

        [HttpPost("repositories/{repositoryId:guid}/insight")]
        [Authorize]
        [EnableRateLimiting(RateLimitPolicyNames.AiExpensive)]
        public async Task<IActionResult> GenerateRepositoryInsight(
            Guid repositoryId,
            [FromQuery] bool force = false,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();

            var insight = await _repoInsightService.GenerateInsightAsync(
                userId,
                repositoryId,
                force,
                cancellationToken);

            return Ok(insight);
        }

        private Guid GetCurrentUserId()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(currentUserId, out var userId))
            {
                throw new InvalidOperationException("Invalid user id");
            }

            return userId;
        }
    }
}
