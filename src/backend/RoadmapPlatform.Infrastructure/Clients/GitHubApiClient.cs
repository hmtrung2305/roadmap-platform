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

        // Create a GET request to GitHub's public repository endpoint
        // sort=updated means GitHub returns recently updated repos first
        // per_page=20 means return up to 20 repos in one request
        public async Task<List<GitHubRepositorySyncDto>> GetPublicRepositoriesAsync(string username)
        {
            // Create an HttpClient instance
            var client = _httpClientFactory.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://api.github.com/users/{username}/repos?sort=updated&per_page=20");

            // GitHub expects a User-Agent header
            // Without this, GitHub may reject the request
            request.Headers.UserAgent.ParseAdd("Roadmap Platform");

            // Tell GitHub we want the official GitHub JSON response format
            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            var response = await client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new NotFoundException("GitHub user was not found");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("Failed to fetch GitHub repositories");
            }

            // Read GitHub's JSON response as a string
            var json = await response.Content.ReadAsStringAsync();

            // Convert the JSON string into C# objects
            // GitHubRepoApiResponse matches GitHub's raw JSON structure
            var repos = JsonSerializer.Deserialize<List<GitHubRepoApiResponse>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Convert GitHub's API raw response model into GitHubRepositoryDto
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

        // Represents the raw JSON shape returned by GitHub's API
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
