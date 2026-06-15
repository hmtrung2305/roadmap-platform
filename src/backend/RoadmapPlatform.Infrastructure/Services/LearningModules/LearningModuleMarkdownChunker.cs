using Microsoft.Extensions.Options;
using RoadmapPlatform.Infrastructure.Configurations;
using System.Text;

namespace RoadmapPlatform.Infrastructure.Services.LearningModules;

public sealed class LearningModuleMarkdownChunker
{
    private readonly LearningModuleRagSettings _settings;

    public LearningModuleMarkdownChunker(IOptions<LearningModuleRagSettings> options)
    {
        _settings = options.Value;
    }

    public IReadOnlyList<LearningModuleMarkdownChunk> Chunk(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return [];
        }

        var sections = SplitIntoHeadingSections(markdown);
        var chunks = new List<LearningModuleMarkdownChunk>();

        foreach (var section in sections)
        {
            if (section.Content.Length <= _settings.MaxChunkCharacters)
            {
                chunks.Add(section);
                continue;
            }

            chunks.AddRange(SplitLongSection(section));
        }

        return chunks
            .Where(chunk => !string.IsNullOrWhiteSpace(chunk.Content))
            .Select((chunk, index) => chunk with
            {
                ChunkIndex = index + 1,
                Content = chunk.Content.Trim()
            })
            .ToList();
    }

    private static IReadOnlyList<LearningModuleMarkdownChunk> SplitIntoHeadingSections(string markdown)
    {
        var normalized = markdown.Replace("\r\n", "\n");
        var lines = normalized.Split('\n');

        var sections = new List<LearningModuleMarkdownChunk>();
        var builder = new StringBuilder();
        string? currentHeading = null;
        var insideCodeFence = false;

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();

            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                insideCodeFence = !insideCodeFence;
            }

            var isHeading = !insideCodeFence && trimmed.StartsWith("#", StringComparison.Ordinal);

            if (isHeading && builder.Length > 0)
            {
                sections.Add(new LearningModuleMarkdownChunk
                {
                    Heading = currentHeading,
                    Content = builder.ToString().Trim()
                });

                builder.Clear();
            }

            if (isHeading)
            {
                currentHeading = trimmed.TrimStart('#').Trim();
            }

            builder.AppendLine(line);
        }

        if (builder.Length > 0)
        {
            sections.Add(new LearningModuleMarkdownChunk
            {
                Heading = currentHeading,
                Content = builder.ToString().Trim()
            });
        }

        return sections;
    }

    private IReadOnlyList<LearningModuleMarkdownChunk> SplitLongSection(
        LearningModuleMarkdownChunk section)
    {
        var paragraphs = section.Content
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var chunks = new List<LearningModuleMarkdownChunk>();
        var builder = new StringBuilder();

        foreach (var paragraph in paragraphs)
        {
            if (builder.Length + paragraph.Length + 2 > _settings.TargetChunkCharacters
                && builder.Length > 0)
            {
                chunks.Add(new LearningModuleMarkdownChunk
                {
                    Heading = section.Heading,
                    Content = builder.ToString().Trim()
                });

                builder.Clear();
            }

            if (paragraph.Length > _settings.MaxChunkCharacters)
            {
                chunks.AddRange(SplitVeryLongParagraph(section.Heading, paragraph));
                continue;
            }

            builder.AppendLine(paragraph);
            builder.AppendLine();
        }

        if (builder.Length > 0)
        {
            chunks.Add(new LearningModuleMarkdownChunk
            {
                Heading = section.Heading,
                Content = builder.ToString().Trim()
            });
        }

        return chunks;
    }

    private IReadOnlyList<LearningModuleMarkdownChunk> SplitVeryLongParagraph(
        string? heading,
        string paragraph)
    {
        var chunks = new List<LearningModuleMarkdownChunk>();

        for (var start = 0; start < paragraph.Length; start += _settings.TargetChunkCharacters)
        {
            var length = Math.Min(_settings.TargetChunkCharacters, paragraph.Length - start);

            chunks.Add(new LearningModuleMarkdownChunk
            {
                Heading = heading,
                Content = paragraph.Substring(start, length)
            });
        }

        return chunks;
    }
}

public sealed record LearningModuleMarkdownChunk
{
    public int ChunkIndex { get; init; }
    public string? Heading { get; init; }
    public string Content { get; init; } = string.Empty;
}
