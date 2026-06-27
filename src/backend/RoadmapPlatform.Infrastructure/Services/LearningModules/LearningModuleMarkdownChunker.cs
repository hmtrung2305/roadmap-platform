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
            return new List<LearningModuleMarkdownChunk>();
        }

        var normalized = markdown.Replace("\r\n", "\n").Trim();

        if (normalized.Length <= _settings.MaxChunkCharacters)
        {
            return new List<LearningModuleMarkdownChunk>
            {
                new()
                {
                    ChunkIndex = 1,
                    Heading = ExtractFirstHeading(normalized),
                    Content = normalized
                }
            };
        }

        var sections = SplitIntoHeadingSections(normalized);
        var chunks = MergeAndSplitSections(sections);

        return chunks
            .Where(chunk => !string.IsNullOrWhiteSpace(chunk.Content))
            .Select((chunk, index) => chunk with
            {
                ChunkIndex = index + 1,
                Content = chunk.Content.Trim()
            })
            .ToList();
    }

    private IReadOnlyList<LearningModuleMarkdownChunk> MergeAndSplitSections(
        IReadOnlyList<LearningModuleMarkdownChunk> sections)
    {
        var chunks = new List<LearningModuleMarkdownChunk>();
        var builder = new StringBuilder();
        string? currentHeading = null;
        List<string> currentBlocks = new();
        string? pendingOverlap = null;

        foreach (var section in sections)
        {
            if (section.Content.Length > _settings.MaxChunkCharacters)
            {
                FlushCurrentChunk();

                var splitChunks = SplitLongSection(section);

                foreach (var splitChunk in splitChunks)
                {
                    chunks.Add(splitChunk);
                }

                pendingOverlap = null;
                continue;
            }

            var projectedLength = builder.Length + section.Content.Length + 2;

            if (projectedLength > _settings.TargetChunkCharacters && currentBlocks.Count > 0)
            {
                FlushCurrentChunk();
            }

            if (currentBlocks.Count == 0
                && !string.IsNullOrWhiteSpace(pendingOverlap)
                && pendingOverlap.Length + section.Content.Length + 2 <= _settings.MaxChunkCharacters)
            {
                AppendBlock(pendingOverlap);
                pendingOverlap = null;
            }

            currentHeading ??= section.Heading;
            AppendBlock(section.Content);
        }

        FlushCurrentChunk(addOverlapForNextChunk: false);

        return chunks;

        void AppendBlock(string block)
        {
            var normalizedBlock = block.Trim();

            if (string.IsNullOrWhiteSpace(normalizedBlock))
            {
                return;
            }

            builder.AppendLine(normalizedBlock);
            builder.AppendLine();
            currentBlocks.Add(normalizedBlock);
        }

        void FlushCurrentChunk(bool addOverlapForNextChunk = true)
        {
            if (currentBlocks.Count == 0)
            {
                return;
            }

            chunks.Add(new LearningModuleMarkdownChunk
            {
                Heading = currentHeading,
                Content = builder.ToString().Trim()
            });

            pendingOverlap = addOverlapForNextChunk
                ? BuildOverlapSeed(currentBlocks)
                : null;

            builder.Clear();
            currentBlocks.Clear();
            currentHeading = null;
        }
    }

    private IReadOnlyList<LearningModuleMarkdownChunk> SplitLongSection(
        LearningModuleMarkdownChunk section)
    {
        var blocks = SplitIntoMarkdownBlocks(section.Content);
        var chunks = new List<LearningModuleMarkdownChunk>();
        var builder = new StringBuilder();
        var currentBlocks = new List<string>();
        string? pendingOverlap = null;

        foreach (var block in blocks)
        {
            if (block.Length > _settings.MaxChunkCharacters)
            {
                FlushCurrentChunk();

                foreach (var chunk in SplitVeryLongBlock(section.Heading, block))
                {
                    chunks.Add(chunk);
                }

                pendingOverlap = null;
                continue;
            }

            var projectedLength = builder.Length + block.Length + 2;

            if (projectedLength > _settings.TargetChunkCharacters && currentBlocks.Count > 0)
            {
                FlushCurrentChunk();
            }

            if (currentBlocks.Count == 0
                && !string.IsNullOrWhiteSpace(pendingOverlap)
                && pendingOverlap.Length + block.Length + 2 <= _settings.MaxChunkCharacters)
            {
                AppendBlock(pendingOverlap);
                pendingOverlap = null;
            }

            AppendBlock(block);
        }

        FlushCurrentChunk(addOverlapForNextChunk: false);

        return chunks;

        void AppendBlock(string block)
        {
            var normalizedBlock = block.Trim();

            if (string.IsNullOrWhiteSpace(normalizedBlock))
            {
                return;
            }

            builder.AppendLine(normalizedBlock);
            builder.AppendLine();
            currentBlocks.Add(normalizedBlock);
        }

        void FlushCurrentChunk(bool addOverlapForNextChunk = true)
        {
            if (currentBlocks.Count == 0)
            {
                return;
            }

            chunks.Add(new LearningModuleMarkdownChunk
            {
                Heading = section.Heading,
                Content = builder.ToString().Trim()
            });

            pendingOverlap = addOverlapForNextChunk
                ? BuildOverlapSeed(currentBlocks)
                : null;

            builder.Clear();
            currentBlocks.Clear();
        }
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

    private static IReadOnlyList<string> SplitIntoMarkdownBlocks(string content)
    {
        var normalized = content.Replace("\r\n", "\n");
        var lines = normalized.Split('\n');

        var blocks = new List<string>();
        var builder = new StringBuilder();
        var insideCodeFence = false;

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();

            if (!insideCodeFence && string.IsNullOrWhiteSpace(line))
            {
                FlushBlock();
                continue;
            }

            builder.AppendLine(line);

            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                insideCodeFence = !insideCodeFence;
            }
        }

        FlushBlock();
        return blocks;

        void FlushBlock()
        {
            if (builder.Length == 0)
            {
                return;
            }

            var block = builder.ToString().Trim();

            if (!string.IsNullOrWhiteSpace(block))
            {
                blocks.Add(block);
            }

            builder.Clear();
        }
    }

    private string? BuildOverlapSeed(IReadOnlyList<string> blocks)
    {
        var overlapCharacters = Math.Max(0, _settings.OverlapCharacters);
        var maxOverlapBlocks = Math.Max(0, _settings.MaxOverlapBlocks);

        if (overlapCharacters == 0 || maxOverlapBlocks == 0 || blocks.Count == 0)
        {
            return null;
        }

        var overlapBlocks = new List<string>();
        var remainingCharacters = overlapCharacters;

        for (var index = blocks.Count - 1; index >= 0; index--)
        {
            if (overlapBlocks.Count >= maxOverlapBlocks || remainingCharacters <= 0)
            {
                break;
            }

            var block = blocks[index].Trim();

            if (string.IsNullOrWhiteSpace(block) || IsFencedCodeBlock(block))
            {
                continue;
            }

            if (block.Length > remainingCharacters)
            {
                block = block[^remainingCharacters..];
            }

            overlapBlocks.Insert(0, block);
            remainingCharacters -= block.Length;
        }

        return overlapBlocks.Count == 0
            ? null
            : string.Join("\n\n", overlapBlocks);
    }

    private IReadOnlyList<LearningModuleMarkdownChunk> SplitVeryLongBlock(
        string? heading,
        string block)
    {
        var chunks = new List<LearningModuleMarkdownChunk>();

        var chunkSize = Math.Min(
            Math.Max(1, _settings.TargetChunkCharacters),
            Math.Max(1, _settings.MaxChunkCharacters));

        var overlap = Math.Min(
            Math.Max(0, _settings.OverlapCharacters),
            chunkSize / 4);

        var step = Math.Max(1, chunkSize - overlap);

        for (var start = 0; start < block.Length; start += step)
        {
            var length = Math.Min(chunkSize, block.Length - start);

            chunks.Add(new LearningModuleMarkdownChunk
            {
                Heading = heading,
                Content = block.Substring(start, length).Trim()
            });

            if (start + length >= block.Length)
            {
                break;
            }
        }

        return chunks;
    }

    private static string? ExtractFirstHeading(string markdown)
    {
        foreach (var line in markdown.Split('\n'))
        {
            var trimmed = line.TrimStart();

            if (trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                return trimmed.TrimStart('#').Trim();
            }
        }

        return null;
    }

    private static bool IsFencedCodeBlock(string block)
    {
        var trimmed = block.Trim();

        return trimmed.StartsWith("```", StringComparison.Ordinal)
            && trimmed.EndsWith("```", StringComparison.Ordinal)
            && trimmed.Length > 6;
    }
}

public sealed record LearningModuleMarkdownChunk
{
    public int ChunkIndex { get; init; }

    public string? Heading { get; init; }

    public string Content { get; init; } = string.Empty;
}