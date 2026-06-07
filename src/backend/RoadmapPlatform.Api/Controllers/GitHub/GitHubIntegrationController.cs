using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Application.Interfaces.GitHub;
using System.Security.Claims;

namespace RoadmapPlatform.Api.Controllers.GitHub
{
    [Route("api/integrations/github")]
    [ApiController]
    public class GitHubIntegrationController : ControllerBase
    {
        private readonly IGitHubRepositoryService _gitHubRepositoryService;

        public GitHubIntegrationController(IGitHubRepositoryService gitHubRepositoryService)
        {
            _gitHubRepositoryService = gitHubRepositoryService;
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
