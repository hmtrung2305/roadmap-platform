using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Api.Constants;
using RoadmapPlatform.Api.Extensions;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.Interfaces.GitHub;

namespace RoadmapPlatform.Api.Controllers.GitHub
{
    /// <summary>
    /// Synchronizes GitHub repositories and generates repository insights.
    /// </summary>
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
        [RequirePermission(PermissionConstant.REPOSITORY_VIEW_SELF)]
        public async Task<IActionResult> GetSavedRepositories(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            var repositories = await _gitHubRepositoryService
                .GetSavedRepositoriesAsync(userId, cancellationToken);

            return Ok(repositories);
        }

        [HttpPost("repositories/sync")]
        [RequirePermission(PermissionConstant.REPOSITORY_SYNC_SELF)]
        [EnableRateLimiting(RateLimitPolicyNames.ExternalApi)]
        public async Task<IActionResult> SyncRepositories(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            var repositories = await _gitHubRepositoryService
                .SyncPublicRepositoriesAsync(userId, cancellationToken);

            return Ok(repositories);
        }

        [HttpPost("repositories/{repositoryId:guid}/insight")]
        [RequirePermission(PermissionConstant.REPO_INSIGHT_GENERATE_SELF)]
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
