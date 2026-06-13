using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.LearningModules;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.LearningModules;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace RoadmapPlatform.Infrastructure.Services.LearningModules;

public sealed class LearningModuleLessonService : ILearningModuleLessonService
{
    private readonly ApplicationDbContext _context;
    private readonly ILearningModuleFileStorage _fileStorage;
    private readonly ILearningModuleRagIndexingService _ragIndexingService;

    public LearningModuleLessonService(
        ApplicationDbContext context,
        ILearningModuleFileStorage fileStorage,
        ILearningModuleRagIndexingService ragIndexingService)
    {
        _context = context;
        _fileStorage = fileStorage;
        _ragIndexingService = ragIndexingService;
    }

    public async Task<BulkUploadLessonsResultDto> BulkUploadLessonsAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        BulkUploadLessonsRequestDto request,
        IReadOnlyList<LearningModuleUploadedFileDto> files,
        CancellationToken cancellationToken)
    {
        var module = await GetOwnedDraftModuleAsync(
            counselorUserId,
            skillModuleId,
            cancellationToken);

        ValidateBulkUploadRequest(request, files);

        var filesByName = files.ToDictionary(
            file => file.FileName,
            StringComparer.OrdinalIgnoreCase);

        var now = DateTime.UtcNow;
        var createdLessons = new List<BulkUploadedLessonDto>();

        foreach (var item in request.Lessons)
        {
            var file = filesByName[item.FileName];

            var markdown = await ReadMarkdownAsync(file.Content, cancellationToken);
            ValidateMarkdownFile(file, markdown);

            var lessonId = Guid.NewGuid();
            var slug = await CreateUniqueLessonSlugAsync(
                skillModuleId,
                string.IsNullOrWhiteSpace(item.Slug) ? item.Title : item.Slug,
                excludeLessonId: null,
                cancellationToken);

            var safeFileName = CreateSafeFileName(file.FileName);
            var objectPath = $"learning-modules/{skillModuleId}/lessons/{lessonId}-{safeFileName}";

            await using var saveStream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));
            var storedFile = await _fileStorage.SaveAsync(
                objectPath,
                saveStream,
                file.ContentType,
                cancellationToken);

            var lesson = new SkillModuleLesson
            {
                SkillModuleLessonId = lessonId,
                SkillModuleId = skillModuleId,
                Title = item.Title.Trim(),
                Slug = slug,
                Summary = NormalizeOptionalText(item.Summary),
                OrderIndex = item.OrderIndex,
                EstimatedHours = item.EstimatedHours,
                MarkdownFileKey = storedFile.ObjectPath,
                MarkdownFileName = file.FileName,
                ContentHash = storedFile.ContentHash ?? CalculateSha256(markdown),
                ContentSizeBytes = storedFile.SizeBytes,
                ContentVersion = 1,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.SkillModuleLessons.Add(lesson);
            await _context.SaveChangesAsync(cancellationToken);

            var chunks = await _ragIndexingService.IndexLessonAsync(
                skillModuleId,
                lessonId,
                markdown,
                cancellationToken);

            createdLessons.Add(new BulkUploadedLessonDto
            {
                ClientId = item.ClientId,
                SkillModuleLessonId = lesson.SkillModuleLessonId,
                Title = lesson.Title,
                Slug = lesson.Slug,
                OrderIndex = lesson.OrderIndex,
                MarkdownFileName = lesson.MarkdownFileName ?? file.FileName,
                MarkdownFileKey = lesson.MarkdownFileKey,
                ContentHash = lesson.ContentHash,
                ContentSizeBytes = lesson.ContentSizeBytes ?? file.Length,
                ContentVersion = lesson.ContentVersion,
                ChunksGenerated = chunks.Count
            });
        }

        module.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return new BulkUploadLessonsResultDto
        {
            Lessons = createdLessons
        };
    }

    public async Task<IReadOnlyList<LearningModuleLessonDto>> ReorderLessonsAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        ReorderLessonsRequestDto request,
        CancellationToken cancellationToken)
    {
        var module = await GetOwnedDraftModuleAsync(
            counselorUserId,
            skillModuleId,
            cancellationToken);

        var lessons = await _context.SkillModuleLessons
            .Include(lesson => lesson.SkillModuleChunks)
            .Where(lesson => lesson.SkillModuleId == skillModuleId)
            .ToListAsync(cancellationToken);

        ValidateReorderRequest(request, lessons);

        var orderMap = request.Lessons.ToDictionary(
            item => item.SkillModuleLessonId,
            item => item.OrderIndex);

        var now = DateTime.UtcNow;

        foreach (var lesson in lessons)
        {
            lesson.OrderIndex = orderMap[lesson.SkillModuleLessonId];
            lesson.UpdatedAt = now;
        }

        module.UpdatedAt = now;

        await _context.SaveChangesAsync(cancellationToken);

        return lessons
            .OrderBy(lesson => lesson.OrderIndex)
            .Select(MapLesson)
            .ToList();
    }

    public async Task<LearningModuleLessonDto> UpdateLessonAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        Guid lessonId,
        UpdateLearningModuleLessonRequestDto request,
        CancellationToken cancellationToken)
    {
        var module = await GetOwnedDraftModuleAsync(
            counselorUserId,
            skillModuleId,
            cancellationToken);

        var lesson = await _context.SkillModuleLessons
            .Include(item => item.SkillModuleChunks)
            .FirstOrDefaultAsync(item =>
                item.SkillModuleLessonId == lessonId
                && item.SkillModuleId == skillModuleId,
                cancellationToken);

        if (lesson == null)
        {
            throw new NotFoundException("Learning module lesson was not found.");
        }

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            lesson.Title = request.Title.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Slug))
        {
            lesson.Slug = await CreateUniqueLessonSlugAsync(
                skillModuleId,
                request.Slug,
                lessonId,
                cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(request.Title))
        {
            lesson.Slug = await CreateUniqueLessonSlugAsync(
                skillModuleId,
                request.Title,
                lessonId,
                cancellationToken);
        }

        if (request.Summary != null)
        {
            lesson.Summary = NormalizeOptionalText(request.Summary);
        }

        if (request.EstimatedHours.HasValue)
        {
            lesson.EstimatedHours = request.EstimatedHours;
        }

        var now = DateTime.UtcNow;
        lesson.UpdatedAt = now;
        module.UpdatedAt = now;

        await _context.SaveChangesAsync(cancellationToken);

        return MapLesson(lesson);
    }

    public async Task<LearningModuleLessonDto> ReplaceLessonContentAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        Guid lessonId,
        LearningModuleUploadedFileDto file,
        CancellationToken cancellationToken)
    {
        var module = await GetOwnedDraftModuleAsync(
            counselorUserId,
            skillModuleId,
            cancellationToken);

        var lesson = await _context.SkillModuleLessons
            .Include(item => item.SkillModuleChunks)
            .FirstOrDefaultAsync(item =>
                item.SkillModuleLessonId == lessonId
                && item.SkillModuleId == skillModuleId,
                cancellationToken);

        if (lesson == null)
        {
            throw new NotFoundException("Learning module lesson was not found.");
        }

        var markdown = await ReadMarkdownAsync(file.Content, cancellationToken);
        ValidateMarkdownFile(file, markdown);

        var oldFileKey = lesson.MarkdownFileKey;

        var safeFileName = CreateSafeFileName(file.FileName);
        var objectPath = $"learning-modules/{skillModuleId}/lessons/{lessonId}-{safeFileName}";

        await using var saveStream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));
        var storedFile = await _fileStorage.SaveAsync(
            objectPath,
            saveStream,
            file.ContentType,
            cancellationToken);

        var chunks = await _ragIndexingService.IndexLessonAsync(
            skillModuleId,
            lessonId,
            markdown,
            cancellationToken);

        lesson.MarkdownFileKey = storedFile.ObjectPath;
        lesson.MarkdownFileName = file.FileName;
        lesson.ContentHash = storedFile.ContentHash ?? CalculateSha256(markdown);
        lesson.ContentSizeBytes = storedFile.SizeBytes;
        lesson.ContentVersion += 1;
        lesson.UpdatedAt = DateTime.UtcNow;

        module.UpdatedAt = lesson.UpdatedAt;

        await _context.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(oldFileKey)
            && !string.Equals(oldFileKey, lesson.MarkdownFileKey, StringComparison.Ordinal))
        {
            await _fileStorage.DeleteAsync(oldFileKey, cancellationToken);
        }

        lesson.SkillModuleChunks = chunks
            .Select(chunk => new SkillModuleChunk
            {
                SkillModuleChunkId = chunk.SkillModuleChunkId,
                SkillModuleId = chunk.SkillModuleId,
                SkillModuleLessonId = chunk.SkillModuleLessonId,
                ChunkIndex = chunk.ChunkIndex,
                Heading = chunk.Heading,
                Content = chunk.Content,
                TokenCount = chunk.TokenCount,
                ContentHash = chunk.ContentHash
            })
            .ToList();

        return MapLesson(lesson);
    }

    public async Task<LearningModuleLessonContentDto> GetLessonPreviewAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        await EnsureOwnedModuleExistsAsync(
            counselorUserId,
            skillModuleId,
            cancellationToken);

        var lesson = await _context.SkillModuleLessons
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.SkillModuleLessonId == lessonId
                && item.SkillModuleId == skillModuleId,
                cancellationToken);

        if (lesson == null)
        {
            throw new NotFoundException("Learning module lesson was not found.");
        }

        await using var stream = await _fileStorage.OpenReadAsync(
            lesson.MarkdownFileKey,
            cancellationToken);

        using var reader = new StreamReader(stream, Encoding.UTF8);
        var markdown = await reader.ReadToEndAsync(cancellationToken);

        return new LearningModuleLessonContentDto
        {
            SkillModuleLessonId = lesson.SkillModuleLessonId,
            SkillModuleId = lesson.SkillModuleId,
            Title = lesson.Title,
            Slug = lesson.Slug,
            Markdown = markdown,
            ContentVersion = lesson.ContentVersion,
            ContentHash = lesson.ContentHash
        };
    }

    public async Task DeleteDraftLessonAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        var module = await GetOwnedDraftModuleAsync(
            counselorUserId,
            skillModuleId,
            cancellationToken);

        var lesson = await _context.SkillModuleLessons
            .FirstOrDefaultAsync(item =>
                item.SkillModuleLessonId == lessonId
                && item.SkillModuleId == skillModuleId,
                cancellationToken);

        if (lesson == null)
        {
            throw new NotFoundException("Learning module lesson was not found.");
        }

        var fileKey = lesson.MarkdownFileKey;

        _context.SkillModuleLessons.Remove(lesson);

        module.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(fileKey))
        {
            await _fileStorage.DeleteAsync(fileKey, cancellationToken);
        }
    }

    private async Task<SkillModule> GetOwnedDraftModuleAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        CancellationToken cancellationToken)
    {
        var module = await _context.SkillModules
            .FirstOrDefaultAsync(item =>
                item.SkillModuleId == skillModuleId
                && item.CreatedByUserId == counselorUserId,
                cancellationToken);

        if (module == null)
        {
            throw new NotFoundException("Learning module was not found.");
        }

        if (module.Status != LearningModuleStatusValues.Draft)
        {
            throw new ConflictException("Only draft modules can be edited.");
        }

        return module;
    }

    private async Task EnsureOwnedModuleExistsAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        CancellationToken cancellationToken)
    {
        var exists = await _context.SkillModules
            .AsNoTracking()
            .AnyAsync(item =>
                item.SkillModuleId == skillModuleId
                && item.CreatedByUserId == counselorUserId,
                cancellationToken);

        if (!exists)
        {
            throw new NotFoundException("Learning module was not found.");
        }
    }

    private static void ValidateBulkUploadRequest(
        BulkUploadLessonsRequestDto request,
        IReadOnlyList<LearningModuleUploadedFileDto> files)
    {
        if (request.Lessons.Count == 0)
        {
            throw new ConflictException("At least one lesson is required.");
        }

        if (files.Count == 0)
        {
            throw new ConflictException("At least one Markdown file is required.");
        }

        var duplicateClientIds = request.Lessons
            .GroupBy(item => item.ClientId)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateClientIds.Count > 0)
        {
            throw new ConflictException("Lesson client IDs must be unique.");
        }

        var duplicateOrderValues = request.Lessons
            .Where(item => item.OrderIndex > 0)
            .GroupBy(item => item.OrderIndex)
            .Any(group => group.Count() > 1);

        if (duplicateOrderValues)
        {
            throw new ConflictException("Lesson order values must be unique.");
        }

        var fileNames = files
            .Select(file => file.FileName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var lesson in request.Lessons)
        {
            if (string.IsNullOrWhiteSpace(lesson.ClientId))
            {
                throw new ConflictException("Each lesson must have a client ID.");
            }

            if (string.IsNullOrWhiteSpace(lesson.Title))
            {
                throw new ConflictException("Each lesson must have a title.");
            }

            if (lesson.OrderIndex <= 0)
            {
                throw new ConflictException("Lesson order values must be positive.");
            }

            if (string.IsNullOrWhiteSpace(lesson.FileName)
                || !fileNames.Contains(lesson.FileName))
            {
                throw new ConflictException($"Missing uploaded file for lesson: {lesson.Title}");
            }
        }
    }

    private static void ValidateReorderRequest(
        ReorderLessonsRequestDto request,
        IReadOnlyList<SkillModuleLesson> existingLessons)
    {
        if (request.Lessons.Count == 0)
        {
            throw new ConflictException("Lesson order list cannot be empty.");
        }

        var existingIds = existingLessons
            .Select(lesson => lesson.SkillModuleLessonId)
            .OrderBy(id => id)
            .ToList();

        var requestIds = request.Lessons
            .Select(lesson => lesson.SkillModuleLessonId)
            .OrderBy(id => id)
            .ToList();

        if (!existingIds.SequenceEqual(requestIds))
        {
            throw new ConflictException("Lesson reorder request must include every lesson in the module.");
        }

        if (request.Lessons.Any(lesson => lesson.OrderIndex <= 0))
        {
            throw new ConflictException("Lesson order values must be positive.");
        }

        if (request.Lessons.Select(lesson => lesson.OrderIndex).Distinct().Count() != request.Lessons.Count)
        {
            throw new ConflictException("Lesson order values must be unique.");
        }
    }

    private static void ValidateMarkdownFile(
        LearningModuleUploadedFileDto file,
        string markdown)
    {
        if (file.Length <= 0)
        {
            throw new ConflictException("Markdown file cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(markdown))
        {
            throw new ConflictException("Markdown file content cannot be empty.");
        }

        var extension = Path.GetExtension(file.FileName);

        if (!string.Equals(extension, ".md", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(extension, ".markdown", StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("Only Markdown files are allowed.");
        }
    }

    private static async Task<string> ReadMarkdownAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: false);

        return await reader.ReadToEndAsync(cancellationToken);
    }

    private async Task<string> CreateUniqueLessonSlugAsync(
        Guid skillModuleId,
        string value,
        Guid? excludeLessonId,
        CancellationToken cancellationToken)
    {
        var baseSlug = Slugify(value);

        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            baseSlug = "lesson";
        }

        var slug = baseSlug;
        var suffix = 2;

        while (await _context.SkillModuleLessons.AnyAsync(
            lesson => lesson.SkillModuleId == skillModuleId
                && lesson.Slug == slug
                && (!excludeLessonId.HasValue || lesson.SkillModuleLessonId != excludeLessonId.Value),
            cancellationToken))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix++;
        }

        return slug;
    }

    private static string Slugify(string value)
    {
        var lower = value.Trim().ToLowerInvariant();
        var slug = Regex.Replace(lower, @"[^a-z0-9]+", "-");
        slug = Regex.Replace(slug, @"-+", "-").Trim('-');

        return slug.Length <= 200
            ? slug
            : slug[..200].Trim('-');
    }

    private static string CreateSafeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        var safe = Regex.Replace(name, @"[^a-zA-Z0-9._-]+", "-");
        return string.IsNullOrWhiteSpace(safe)
            ? "lesson.md"
            : safe;
    }

    private static string CalculateSha256(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static LearningModuleLessonDto MapLesson(SkillModuleLesson lesson)
    {
        return new LearningModuleLessonDto
        {
            SkillModuleLessonId = lesson.SkillModuleLessonId,
            SkillModuleId = lesson.SkillModuleId,
            Title = lesson.Title,
            Slug = lesson.Slug,
            Summary = lesson.Summary,
            OrderIndex = lesson.OrderIndex,
            EstimatedHours = lesson.EstimatedHours,
            MarkdownFileKey = lesson.MarkdownFileKey,
            MarkdownFileName = lesson.MarkdownFileName,
            ContentHash = lesson.ContentHash,
            ContentSizeBytes = lesson.ContentSizeBytes,
            ContentVersion = lesson.ContentVersion,
            ChunkCount = lesson.SkillModuleChunks.Count,
            CreatedAt = lesson.CreatedAt,
            UpdatedAt = lesson.UpdatedAt
        };
    }
}
