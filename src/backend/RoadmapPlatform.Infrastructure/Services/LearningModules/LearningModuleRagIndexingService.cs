using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pgvector;
using RoadmapPlatform.Application.DTOs.LearningModules;
using RoadmapPlatform.Application.Interfaces.LearningModules;
using RoadmapPlatform.Infrastructure.Configurations;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using System.Security.Cryptography;
using System.Text;

namespace RoadmapPlatform.Infrastructure.Services.LearningModules;

public sealed class LearningModuleRagIndexingService : ILearningModuleRagIndexingService
{
    private readonly ApplicationDbContext _context;
    private readonly LearningModuleMarkdownChunker _chunker;
    private readonly LearningModuleRagSettings _ragSettings;
    private readonly AiSettings _aiSettings;
    private readonly Client _client;

    public LearningModuleRagIndexingService(
        ApplicationDbContext context,
        LearningModuleMarkdownChunker chunker,
        IOptions<LearningModuleRagSettings> ragOptions,
        IOptions<AiSettings> aiOptions)
    {
        _context = context;
        _chunker = chunker;
        _ragSettings = ragOptions.Value;
        _aiSettings = aiOptions.Value;

        if (string.IsNullOrWhiteSpace(_aiSettings.ApiKey))
        {
            throw new InvalidOperationException("Gemini API key was not configured.");
        }

        _client = new Client(apiKey: _aiSettings.ApiKey);
    }

    public async Task<IReadOnlyList<LearningModuleChunkDto>> IndexLessonAsync(
        Guid skillModuleId,
        Guid skillModuleLessonId,
        string markdown,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            throw new InvalidOperationException("Lesson Markdown content cannot be empty.");
        }

        var lessonExists = await _context.SkillModuleLessons
            .AsNoTracking()
            .AnyAsync(lesson =>
                lesson.SkillModuleLessonId == skillModuleLessonId
                && lesson.SkillModuleId == skillModuleId,
                cancellationToken);

        if (!lessonExists)
        {
            throw new KeyNotFoundException("Learning module lesson was not found.");
        }

        await DeleteLessonChunksAsync(skillModuleLessonId, cancellationToken);

        var markdownChunks = _chunker.Chunk(markdown);
        var entities = new List<SkillModuleChunk>();

        foreach (var chunk in markdownChunks)
        {
            var embedding = await CreateEmbeddingAsync(chunk.Content, cancellationToken);
            var contentHash = CalculateSha256(chunk.Content);

            entities.Add(new SkillModuleChunk
            {
                SkillModuleChunkId = Guid.NewGuid(),
                SkillModuleId = skillModuleId,
                SkillModuleLessonId = skillModuleLessonId,
                ChunkIndex = chunk.ChunkIndex,
                Heading = chunk.Heading,
                Content = chunk.Content,
                Embedding = new Vector(embedding),
                TokenCount = EstimateTokenCount(chunk.Content),
                ContentHash = contentHash,
                CreatedAt = DateTime.UtcNow
            });
        }

        _context.SkillModuleChunks.AddRange(entities);
        await _context.SaveChangesAsync(cancellationToken);

        return entities
            .Select(MapChunk)
            .ToList();
    }

    public async Task DeleteLessonChunksAsync(
        Guid skillModuleLessonId,
        CancellationToken cancellationToken)
    {
        var chunks = await _context.SkillModuleChunks
            .Where(chunk => chunk.SkillModuleLessonId == skillModuleLessonId)
            .ToListAsync(cancellationToken);

        if (chunks.Count == 0)
        {
            return;
        }

        _context.SkillModuleChunks.RemoveRange(chunks);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LearningModuleRagSourceDto>> SearchRelevantChunksAsync(
        Guid skillModuleId,
        Guid? preferredLessonId,
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var queryEmbedding = await CreateEmbeddingAsync(query, cancellationToken);

        var chunks = await _context.SkillModuleChunks
            .AsNoTracking()
            .Include(chunk => chunk.SkillModuleLesson)
            .Where(chunk => chunk.SkillModuleId == skillModuleId)
            .Where(chunk => chunk.Embedding != null)
            .ToListAsync(cancellationToken);

        if (chunks.Count == 0)
        {
            return [];
        }

        var maxResults = limit > 0
            ? limit
            : _ragSettings.MaxChunks;

        return chunks
            .Select(chunk => new
            {
                Chunk = chunk,
                Score = CalculateCosineSimilarity(
                    queryEmbedding,
                    chunk.Embedding!.ToArray()),
                LessonPriority = preferredLessonId.HasValue
                    && chunk.SkillModuleLessonId == preferredLessonId.Value
                        ? 1
                        : 0
            })
            .Where(item => item.Score >= _ragSettings.SimilarityThreshold)
            .OrderByDescending(item => item.LessonPriority)
            .ThenByDescending(item => item.Score)
            .Take(maxResults)
            .Select(item => new LearningModuleRagSourceDto
            {
                SkillModuleChunkId = item.Chunk.SkillModuleChunkId,
                SkillModuleLessonId = item.Chunk.SkillModuleLessonId,
                LessonTitle = item.Chunk.SkillModuleLesson.Title,
                Heading = item.Chunk.Heading,
                ContentPreview = CreatePreview(item.Chunk.Content),
                SimilarityScore = item.Score
            })
            .ToList();
    }

    private async Task<float[]> CreateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken)
    {
        var response = await _client.Models.EmbedContentAsync(
            model: string.IsNullOrWhiteSpace(_aiSettings.EmbeddingModel)
                ? "gemini-embedding-2"
                : _aiSettings.EmbeddingModel,
            contents: text,
            config: new EmbedContentConfig
            {
                OutputDimensionality = _ragSettings.EmbeddingDimensions
            });

        var values = response.Embeddings?[0]?.Values?
            .Select(value => (float)value)
            .ToArray();

        if (values == null || values.Length == 0)
        {
            throw new InvalidOperationException("Could not generate embedding.");
        }

        return values;
    }

    private static LearningModuleChunkDto MapChunk(SkillModuleChunk chunk)
    {
        return new LearningModuleChunkDto
        {
            SkillModuleChunkId = chunk.SkillModuleChunkId,
            SkillModuleId = chunk.SkillModuleId,
            SkillModuleLessonId = chunk.SkillModuleLessonId,
            ChunkIndex = chunk.ChunkIndex,
            Heading = chunk.Heading,
            Content = chunk.Content,
            TokenCount = chunk.TokenCount,
            ContentHash = chunk.ContentHash
        };
    }

    private static string CalculateSha256(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static int EstimateTokenCount(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return 0;
        }

        return Math.Max(1, content.Length / 4);
    }

    private static double CalculateCosineSimilarity(float[] vector1, float[] vector2)
    {
        if (vector1.Length != vector2.Length)
        {
            return 0;
        }

        double dotProduct = 0;
        double magnitude1 = 0;
        double magnitude2 = 0;

        for (var i = 0; i < vector1.Length; i++)
        {
            dotProduct += vector1[i] * vector2[i];
            magnitude1 += vector1[i] * vector1[i];
            magnitude2 += vector2[i] * vector2[i];
        }

        if (magnitude1 == 0 || magnitude2 == 0)
        {
            return 0;
        }

        return dotProduct / (Math.Sqrt(magnitude1) * Math.Sqrt(magnitude2));
    }

    private static string CreatePreview(string content)
    {
        const int maxLength = 260;

        var compact = content
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();

        return compact.Length <= maxLength
            ? compact
            : compact[..maxLength] + "...";
    }
}
