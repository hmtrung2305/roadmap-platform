using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
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
    private const double PreferredLessonBoost = 0.05d;
    private const int MaxChunksPerLesson = 2;

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
        int expectedContentVersion,
        string? expectedContentHash,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            throw new InvalidOperationException("Lesson Markdown content cannot be empty.");
        }

        var lesson = await _context.SkillModuleLessons
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.SkillModuleLessonId == skillModuleLessonId
                && item.SkillModuleId == skillModuleId,
                cancellationToken);

        if (lesson == null)
        {
            throw new KeyNotFoundException("Learning module lesson was not found.");
        }

        ThrowIfLessonContentChanged(
            lesson,
            expectedContentVersion,
            expectedContentHash);

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

        var currentLesson = await _context.SkillModuleLessons
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.SkillModuleLessonId == skillModuleLessonId
                && item.SkillModuleId == skillModuleId,
                cancellationToken);

        if (currentLesson == null)
        {
            throw new KeyNotFoundException("Learning module lesson was not found.");
        }

        ThrowIfLessonContentChanged(
            currentLesson,
            expectedContentVersion,
            expectedContentHash);

        var existingChunks = await _context.SkillModuleChunks
            .Where(chunk => chunk.SkillModuleLessonId == skillModuleLessonId)
            .ToListAsync(cancellationToken);

        if (existingChunks.Count > 0)
        {
            _context.SkillModuleChunks.RemoveRange(existingChunks);
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

        if (queryEmbedding.Length != _ragSettings.EmbeddingDimensions)
        {
            throw new InvalidOperationException(
                $"Generated embedding dimension {queryEmbedding.Length} does not match configured dimension {_ragSettings.EmbeddingDimensions}.");
        }

        var maxResults = limit > 0
            ? limit
            : _ragSettings.MaxChunks;

        return await SearchRelevantChunksWithPgvectorAsync(
            skillModuleId,
            preferredLessonId,
            queryEmbedding,
            maxResults,
            cancellationToken);
    }

    private async Task<IReadOnlyList<LearningModuleRagSourceDto>> SearchRelevantChunksWithPgvectorAsync(
    Guid skillModuleId,
    Guid? preferredLessonId,
    float[] queryEmbedding,
    int maxResults,
    CancellationToken cancellationToken)
    {
        const string sql = """
        WITH eligible_chunks AS (
            SELECT
                c.skill_module_chunk_id,
                c.skill_module_lesson_id,
                l.title AS lesson_title,
                c.heading,
                c.content,
                (1 - (c.embedding <=> @query_embedding))::double precision AS similarity_score,
                (c.embedding <=> @query_embedding)::double precision AS distance
            FROM public.skill_module_chunk AS c
            INNER JOIN public.skill_module_lesson AS l
                ON l.skill_module_lesson_id = c.skill_module_lesson_id
            WHERE c.skill_module_id = @skill_module_id
              AND c.embedding IS NOT NULL
              AND l.indexing_status = @indexed_status
        ),
        global_candidates AS (
            SELECT *
            FROM eligible_chunks
            ORDER BY distance
            LIMIT @candidate_limit
        ),
        preferred_lesson_candidates AS (
            SELECT *
            FROM eligible_chunks
            WHERE CAST(@preferred_lesson_id AS uuid) IS NOT NULL
              AND skill_module_lesson_id = CAST(@preferred_lesson_id AS uuid)
            ORDER BY distance
            LIMIT @preferred_candidate_limit
        ),
        candidate_chunks AS (
            SELECT DISTINCT ON (skill_module_chunk_id)
                skill_module_chunk_id,
                skill_module_lesson_id,
                lesson_title,
                heading,
                content,
                similarity_score,
                distance
            FROM (
                SELECT * FROM global_candidates
                UNION ALL
                SELECT * FROM preferred_lesson_candidates
            ) AS combined_candidates
            ORDER BY skill_module_chunk_id, distance
        ),
        scored_chunks AS (
            SELECT
                *,
                similarity_score +
                    CASE
                        WHEN skill_module_lesson_id =
                             CAST(@preferred_lesson_id AS uuid)
                            THEN @preferred_lesson_boost
                        ELSE 0
                    END AS adjusted_score
            FROM candidate_chunks
            WHERE similarity_score >= @similarity_threshold
        ),
        ranked_chunks AS (
            SELECT
                *,
                ROW_NUMBER() OVER (
                    PARTITION BY skill_module_lesson_id
                    ORDER BY adjusted_score DESC, distance
                ) AS lesson_rank
            FROM scored_chunks
        ),
        diversified_chunks AS (
            SELECT
                *,
                CASE
                    WHEN lesson_rank <= @max_chunks_per_lesson THEN 0
                    ELSE 1
                END AS diversity_group
            FROM ranked_chunks
        )
        SELECT
            skill_module_chunk_id,
            skill_module_lesson_id,
            lesson_title,
            heading,
            content,
            similarity_score
        FROM diversified_chunks
        ORDER BY
            diversity_group,
            adjusted_score DESC,
            distance
        LIMIT @max_results;
        """;

        var candidateLimit = Math.Clamp(maxResults * 4, maxResults, 50);
        var results = new List<LearningModuleRagSourceDto>(maxResults);

        var connection = _context.Database.GetDbConnection();
        var shouldCloseConnection =
            connection.State != System.Data.ConnectionState.Open;

        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;

            command.Parameters.Add(
                new NpgsqlParameter("skill_module_id", NpgsqlDbType.Uuid)
                {
                    Value = skillModuleId
                });

            command.Parameters.Add(
                new NpgsqlParameter("preferred_lesson_id", NpgsqlDbType.Uuid)
                {
                    Value = preferredLessonId.HasValue
                        ? preferredLessonId.Value
                        : DBNull.Value
                });

            command.Parameters.Add(
                new NpgsqlParameter(
                    "query_embedding",
                    new Vector(queryEmbedding)));

            command.Parameters.Add(
                new NpgsqlParameter("indexed_status", NpgsqlDbType.Text)
                {
                    Value = LearningModuleLessonIndexingStatusValues.Indexed
                });

            command.Parameters.Add(
                new NpgsqlParameter("candidate_limit", NpgsqlDbType.Integer)
                {
                    Value = candidateLimit
                });

            command.Parameters.Add(
                new NpgsqlParameter("preferred_candidate_limit", NpgsqlDbType.Integer)
                {
                    Value = maxResults
                });

            command.Parameters.Add(
                new NpgsqlParameter("preferred_lesson_boost", NpgsqlDbType.Double)
                {
                    Value = PreferredLessonBoost
                });

            command.Parameters.Add(
                new NpgsqlParameter("max_chunks_per_lesson", NpgsqlDbType.Integer)
                {
                    Value = MaxChunksPerLesson
                });

            command.Parameters.Add(
                new NpgsqlParameter("similarity_threshold", NpgsqlDbType.Double)
                {
                    Value = (double)_ragSettings.SimilarityThreshold
                });

            command.Parameters.Add(
                new NpgsqlParameter("max_results", NpgsqlDbType.Integer)
                {
                    Value = maxResults
                });

            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            var chunkIdOrdinal =
                reader.GetOrdinal("skill_module_chunk_id");

            var lessonIdOrdinal =
                reader.GetOrdinal("skill_module_lesson_id");

            var lessonTitleOrdinal =
                reader.GetOrdinal("lesson_title");

            var headingOrdinal =
                reader.GetOrdinal("heading");

            var contentOrdinal =
                reader.GetOrdinal("content");

            var similarityOrdinal =
                reader.GetOrdinal("similarity_score");

            while (await reader.ReadAsync(cancellationToken))
            {
                var content = reader.GetString(contentOrdinal);

                results.Add(new LearningModuleRagSourceDto
                {
                    SkillModuleChunkId =
                        reader.GetGuid(chunkIdOrdinal),

                    SkillModuleLessonId =
                        reader.GetGuid(lessonIdOrdinal),

                    LessonTitle =
                        reader.GetString(lessonTitleOrdinal),

                    Heading = reader.IsDBNull(headingOrdinal)
                        ? null
                        : reader.GetString(headingOrdinal),

                    ContentPreview = CreatePreview(content),

                    SimilarityScore =
                        reader.GetDouble(similarityOrdinal)
                });
            }
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }

        return results;
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

    private static void ThrowIfLessonContentChanged(
        SkillModuleLesson lesson,
        int expectedContentVersion,
        string? expectedContentHash)
    {
        if (lesson.ContentVersion == expectedContentVersion
            && string.Equals(
                lesson.ContentHash,
                expectedContentHash,
                StringComparison.Ordinal))
        {
            return;
        }

        throw new LearningModuleIndexingSkippedException(
            "Lesson content changed while indexing. Indexing was skipped.");
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
