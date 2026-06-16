using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RoadmapPlatform.Api.Constants;
using RoadmapPlatform.Api.Extensions;
using RoadmapPlatform.Application.Interfaces.GitHub;

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
        public async Task<IActionResult> GetSavedRepositories(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            var repositories = await _gitHubRepositoryService
                .GetSavedRepositoriesAsync(userId, cancellationToken);

            return Ok(repositories);
        }

        [HttpPost("repositories/sync")]
        [Authorize]
        [EnableRateLimiting(RateLimitPolicyNames.ExternalApi)]
        public async Task<IActionResult> SyncRepositories(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            var repositories = await _gitHubRepositoryService
                .SyncPublicRepositoriesAsync(userId, cancellationToken);

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
            return User.GetUserId();
        }
    }
}
