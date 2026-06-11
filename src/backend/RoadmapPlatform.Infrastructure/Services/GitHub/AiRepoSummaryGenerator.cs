using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Options;
using RoadmapPlatform.Application.DTOs.GitHub;
using RoadmapPlatform.Application.Interfaces.GitHub;
using RoadmapPlatform.Infrastructure.Configurations;
using System.Text.Json;

namespace RoadmapPlatform.Infrastructure.Services.GitHub
{
    public class AiRepoSummaryGenerator : IRepoSummaryGenerator
    {
        private readonly Client _client;
        private readonly AiSettings _aiSettings;

        public AiRepoSummaryGenerator(IOptions<AiSettings> aiOptions)
        {
            _aiSettings = aiOptions.Value;

            if (string.IsNullOrWhiteSpace(_aiSettings.ApiKey))
            {
                throw new InvalidOperationException("Gemini API key was not configured.");
            }

            _client = new Client(apiKey: _aiSettings.ApiKey);
        }

        public async Task<GeneratedRepoInsightDto> GenerateAsync(
            RepoSummaryGenerationRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var model = string.IsNullOrWhiteSpace(_aiSettings.GenerationModel)
                ? "gemini-2.5-flash"
                : _aiSettings.GenerationModel;

            var systemInstruction = "You analyze GitHub repositories for a student developer portfolio. " +
                                    "Return valid JSON only. Do not wrap the JSON in markdown. " +
                                    "Do not invent technologies that are not supported by the repository metadata or README.";

            var prompt = BuildPrompt(request);

            var config = new GenerateContentConfig
            {
                SystemInstruction = new Content
                {
                    Role = "system",
                    Parts = new List<Part>
                    {
                        new Part
                        {
                            Text = systemInstruction
                        }
                    }
                }
            };

            var response = await _client.Models.GenerateContentAsync(
                model: model,
                contents: prompt,
                config: config);

            var responseText = response?.Candidates?[0]?.Content?.Parts?[0]?.Text;

            if (string.IsNullOrWhiteSpace(responseText))
            {
                throw new InvalidOperationException("AI did not return a repository insight response.");
            }

            var parsed = ParseResponse(responseText);
            parsed.AiModel = model;

            return parsed;
        }

        private static string BuildPrompt(RepoSummaryGenerationRequestDto request)
        {
            return $$"""
            Analyze this GitHub repository README and return JSON only.

            <repository-metadata>
            Name: {{request.Name}}
            Full name: {{request.FullName}}
            Description: {{request.Description ?? "No description provided"}}
            Primary language: {{request.PrimaryLanguage ?? "Unknown"}}
            Stars: {{request.Stars}}
            Forks: {{request.Forks}}
            </repository-metadata>

            <readme>
            {{request.Readme}}
            </readme>

            Return this exact JSON shape:
            {
              "summary": "2-3 concise portfolio-friendly project summary sentences",
              "techStack": ["technology names only"],
              "detectedSkills": ["skills demonstrated by the project"],
              "projectType": "one concise project category"
            }

            Rules:
            - Keep the summary concise and useful for a public e-portfolio.
            - Use the README as the main source of truth.
            - Use the primary language as a weak hint only.
            - If a technology is unclear, do not include it.
            - techStack must contain 2 to 5 concrete technologies only.
            - detectedSkills must contain 2 to 5 human-readable skills only.
            - techStack examples: React, ASP.NET Core, PostgreSQL, Entity Framework Core, Vite, Docker.
            - detectedSkills examples: Frontend development, REST API design, Database modeling, Authentication implementation, Responsive UI design.
            - Do not put programming languages, frameworks, libraries, databases, tools, or platforms in detectedSkills.
            - Do not duplicate any item between techStack and detectedSkills.
            - projectType should be one of: Full Stack Web Application, Frontend Application, Backend API, Machine Learning Project, Data Analysis Project, Library / Package, CLI Tool, Mobile Application, Other.
            """;
        }

        private static GeneratedRepoInsightDto ParseResponse(string responseText)
        {
            var json = responseText.Trim();

            if (json.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                json = json[7..].Trim();
            }
            else if (json.StartsWith("```", StringComparison.OrdinalIgnoreCase))
            {
                json = json[3..].Trim();
            }

            if (json.EndsWith("```", StringComparison.OrdinalIgnoreCase))
            {
                json = json[..^3].Trim();
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var parsed = JsonSerializer.Deserialize<AiRepoInsightJsonResponse>(json, options);

            if (parsed == null || string.IsNullOrWhiteSpace(parsed.Summary))
            {
                throw new InvalidOperationException("AI repository insight response could not be parsed.");
            }

            var techStack = NormalizeList(parsed.TechStack, maxItems: 5);
            var detectedSkills = NormalizeList(parsed.DetectedSkills, maxItems: 5)
                .Where(skill => !techStack.Contains(skill, StringComparer.OrdinalIgnoreCase))
                .ToList();

            return new GeneratedRepoInsightDto
            {
                Summary = parsed.Summary.Trim(),
                TechStack = techStack,
                DetectedSkills = detectedSkills,
                ProjectType = string.IsNullOrWhiteSpace(parsed.ProjectType)
                    ? "Other"
                    : parsed.ProjectType.Trim()
            };
        }

        private static List<string> NormalizeList(List<string>? values, int maxItems)
        {
            return values?
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(maxItems)
                .ToList() ?? new List<string>();
        }

        private class AiRepoInsightJsonResponse
        {
            public string Summary { get; set; } = string.Empty;

            public List<string>? TechStack { get; set; }

            public List<string>? DetectedSkills { get; set; }

            public string? ProjectType { get; set; }
        }
    }
}