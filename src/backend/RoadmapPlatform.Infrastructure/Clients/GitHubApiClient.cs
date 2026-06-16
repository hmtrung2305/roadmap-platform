using RoadmapPlatform.Application.DTOs.GitHub;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.GitHub;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoadmapPlatform.Infrastructure.Clients
{
    public class GitHubApiClient : IGitHubApiClient
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public GitHubApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // Create a GET request to GitHub's public repository endpoint.
        // sort=updated means GitHub returns recently updated repos first.
        // per_page=20 means return up to 20 repos in one request.
        public async Task<List<GitHubRepositorySyncDto>> GetPublicRepositoriesAsync(
            string username,
            string accessToken,
            CancellationToken cancellationToken = default)
        {
            var client = _httpClientFactory.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://api.github.com/users/{username}/repos?sort=updated&per_page=20");

            AddDefaultGitHubHeaders(request, accessToken);

            var response = await client.SendAsync(request, cancellationToken);
            await EnsureSuccessGitHubResponseAsync(
                response,
                "Failed to fetch GitHub repositories.",
                cancellationToken);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            var repos = JsonSerializer.Deserialize<List<GitHubRepoApiResponse>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return repos?.Where(x => !x.IsPrivate).Select(x => new GitHubRepositorySyncDto
            {
                GithubRepoId = x.Id,
                Name = x.Name,
                FullName = x.FullName,
                HtmlUrl = x.HtmlUrl,
                Description = x.Description,
                PrimaryLanguage = x.Language,
                Stars = x.Stars,
                Forks = x.Forks,
                IsPrivate = x.IsPrivate,
                GithubCreatedAt = x.CreatedAt,
                GithubUpdatedAt = x.UpdatedAt
            }).ToList() ?? new List<GitHubRepositorySyncDto>();
        }

        public async Task<string?> GetRepositoryReadmeAsync(
            string owner,
            string repositoryName,
            string accessToken,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(owner))
            {
                throw new ArgumentException("GitHub repository owner is required.", nameof(owner));
            }

            if (string.IsNullOrWhiteSpace(repositoryName))
            {
                throw new ArgumentException("GitHub repository name is required.", nameof(repositoryName));
            }

            var client = _httpClientFactory.CreateClient();

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://api.github.com/repos/{owner}/{repositoryName}/readme");

            AddDefaultGitHubHeaders(request, accessToken);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github.raw"));

            var response = await client.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            await EnsureSuccessGitHubResponseAsync(
                response,
                "Failed to fetch repository README.",
                cancellationToken);

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }

        private static void AddDefaultGitHubHeaders(HttpRequestMessage request, string accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw GitHubIntegrationException.TokenMissing();
            }

            request.Headers.UserAgent.ParseAdd("Roadmap-Platform");
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        }

        private static async Task EnsureSuccessGitHubResponseAsync(
            HttpResponseMessage response,
            string fallbackMessage,
            CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (IsInvalidTokenResponse(response, responseBody))
            {
                throw GitHubIntegrationException.TokenInvalid();
            }

            if (IsRateLimitedResponse(response, responseBody))
            {
                throw GitHubIntegrationException.RateLimited(GetRetryAfterSeconds(response));
            }

            throw GitHubIntegrationException.ApiFailure(fallbackMessage);
        }

        private static bool IsInvalidTokenResponse(HttpResponseMessage response, string responseBody)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return true;
            }

            return responseBody.Contains("Bad credentials", StringComparison.OrdinalIgnoreCase) ||
                   responseBody.Contains("token expired", StringComparison.OrdinalIgnoreCase) ||
                   responseBody.Contains("invalid token", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsRateLimitedResponse(HttpResponseMessage response, string responseBody)
        {
            if (response.Headers.TryGetValues("X-RateLimit-Remaining", out var remainingValues) &&
                remainingValues.Any(value => value == "0"))
            {
                return true;
            }

            if (response.StatusCode is not HttpStatusCode.Forbidden and not HttpStatusCode.TooManyRequests)
            {
                return false;
            }

            return responseBody.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
                   responseBody.Contains("secondary rate limit", StringComparison.OrdinalIgnoreCase) ||
                   responseBody.Contains("abuse detection", StringComparison.OrdinalIgnoreCase);
        }

        private static int? GetRetryAfterSeconds(HttpResponseMessage response)
        {
            if (response.Headers.RetryAfter?.Delta is { } delta)
            {
                return Math.Max(1, (int)Math.Ceiling(delta.TotalSeconds));
            }

            if (response.Headers.RetryAfter?.Date is { } date)
            {
                var seconds = (int)Math.Ceiling((date - DateTimeOffset.UtcNow).TotalSeconds);
                return seconds > 0 ? seconds : null;
            }

            if (response.Headers.TryGetValues("X-RateLimit-Reset", out var resetValues) &&
                long.TryParse(resetValues.FirstOrDefault(), out var resetUnixSeconds))
            {
                var resetAt = DateTimeOffset.FromUnixTimeSeconds(resetUnixSeconds);
                var seconds = (int)Math.Ceiling((resetAt - DateTimeOffset.UtcNow).TotalSeconds);
                return seconds > 0 ? seconds : null;
            }

            return null;
        }

        private class GitHubRepoApiResponse
        {
            [JsonPropertyName("id")]
            public long Id { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("full_name")]
            public string FullName { get; set; } = string.Empty;

            [JsonPropertyName("html_url")]
            public string HtmlUrl { get; set; } = string.Empty;

            [JsonPropertyName("description")]
            public string? Description { get; set; }

            [JsonPropertyName("language")]
            public string? Language { get; set; }

            [JsonPropertyName("stargazers_count")]
            public int Stars { get; set; }

            [JsonPropertyName("forks_count")]
            public int Forks { get; set; }

            [JsonPropertyName("private")]
            public bool IsPrivate { get; set; }

            [JsonPropertyName("created_at")]
            public DateTime? CreatedAt { get; set; }

            [JsonPropertyName("updated_at")]
            public DateTime? UpdatedAt { get; set; }
        }
    }
}
