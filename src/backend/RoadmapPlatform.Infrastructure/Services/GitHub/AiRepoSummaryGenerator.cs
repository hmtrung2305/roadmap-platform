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

            Classification rules:
            - Use the README as the main source of truth.
            - Use repository metadata as supporting context only.
            - Do not invent features, technologies, architecture, or skills that are not supported by the README or metadata.
            - Separate technologies from skills clearly.
            - techStack should describe what the project was built with.
            - detectedSkills should describe what the developer demonstrated.
            - Do not duplicate the same concept across techStack and detectedSkills.

            techStack rules:
            - Include 2 to 5 concrete technologies.
            - Use short canonical technology names.
            - A technology can be a programming language, framework, library, database, runtime, server, platform, cloud service, package, or development tool.
            - Only include technologies clearly supported by the README or metadata.
            - Valid examples: React, ASP.NET Core, PostgreSQL, Entity Framework Core, Vite, Docker, JSP, MySQL, Apache Tomcat, Bootstrap.
            - Invalid examples: Frontend development, Database integration, Authentication, Responsive UI, REST API design.

            detectedSkills rules:
            - Include 3 to 4 concise skills.
            - Use reusable developer skill names, not long feature descriptions.
            - Skills should be human-readable portfolio labels.
            - Prefer general engineering abilities over overly project-specific wording.
            - Do not include programming languages, frameworks, libraries, databases, servers, tools, or platforms.
            - Valid examples: Full-stack development, Frontend development, Backend development, REST API design, Database design, Database integration, Authentication, Authorization, MVC architecture, Responsive UI, Dashboard development.
            - Invalid examples: React, JSP, MySQL, Apache Tomcat, Bootstrap, Docker, GitHub, JavaScript, C#.
            - Avoid vague skills such as Programming, Coding, Software engineering, Problem solving, or Web development unless the README does not support anything more specific.

            projectType rules:
            - Choose exactly one of these values: Full Stack Web Application, Frontend Application, Backend API, Machine Learning Project, Data Analysis Project, 
              Library / Package, CLI Tool, Mobile Application, Other

            Summary rules:
            - Write for a public e-portfolio.
            - Mention what the project does and what engineering value it demonstrates.
            - Keep the tone professional and factual.
            - Do not exaggerate the project.
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