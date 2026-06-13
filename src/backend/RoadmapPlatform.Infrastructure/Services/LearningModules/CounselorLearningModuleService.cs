using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.LearningModules;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.LearningModules;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RoadmapPlatform.Infrastructure.Services.LearningModules;

public sealed class CounselorLearningModuleService : ICounselorLearningModuleService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ApplicationDbContext _context;

    public CounselorLearningModuleService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CounselorLearningModuleSummaryDto>> GetModulesAsync(
        Guid counselorUserId,
        string? status,
        CancellationToken cancellationToken)
    {
        var query = _context.SkillModules
            .AsNoTracking()
            .Include(module => module.Skill)
            .Include(module => module.SkillModuleLessons)
            .Include(module => module.SkillModuleQuiz)
                .ThenInclude(quiz => quiz!.SkillModuleQuizQuestions)
            .Where(module => module.CreatedByUserId == counselorUserId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(module => module.Status == status);
        }

        var modules = await query.ToListAsync(cancellationToken);

        return modules
            .OrderBy(module => GetStatusSortOrder(module.Status))
            .ThenByDescending(module => GetStatusTime(module))
            .Select(MapSummary)
            .ToList();
    }

    public async Task<CounselorLearningModuleDetailDto> GetModuleDetailAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        CancellationToken cancellationToken)
    {
        var module = await GetOwnedModuleQuery(counselorUserId)
            .Include(item => item.Skill)
            .Include(item => item.SkillModuleLessons)
                .ThenInclude(lesson => lesson.SkillModuleChunks)
            .Include(item => item.SkillModuleQuiz)
                .ThenInclude(quiz => quiz!.SkillModuleQuizQuestions)
                    .ThenInclude(question => question.SkillModuleQuizOptions)
            .FirstOrDefaultAsync(item => item.SkillModuleId == skillModuleId, cancellationToken);

        if (module == null)
        {
            throw new NotFoundException("Learning module was not found.");
        }

        return new CounselorLearningModuleDetailDto
        {
            Module = MapModule(module),
            Lessons = module.SkillModuleLessons
                .OrderBy(lesson => lesson.OrderIndex)
                .Select(MapLesson)
                .ToList(),
            Quiz = module.SkillModuleQuiz == null
                ? null
                : MapQuiz(module.SkillModuleQuiz),
            PublishReadiness = ValidatePublishReadiness(module)
        };
    }

    public async Task<SkillModuleDto> CreateModuleAsync(
        Guid counselorUserId,
        CreateLearningModuleRequestDto request,
        CancellationToken cancellationToken)
    {
        var skill = await _context.Skills
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.SkillId == request.SkillId, cancellationToken);

        if (skill == null)
        {
            throw new NotFoundException("Skill was not found.");
        }

        var slugBase = string.IsNullOrWhiteSpace(request.Slug)
            ? request.Title
            : request.Slug;

        var slug = await CreateUniqueSlugAsync(
            slugBase,
            excludeModuleId: null,
            cancellationToken);

        var now = DateTime.UtcNow;

        var module = new SkillModule
        {
            SkillModuleId = Guid.NewGuid(),
            SkillId = request.SkillId,
            Title = request.Title.Trim(),
            Slug = slug,
            Description = NormalizeOptionalText(request.Description),
            DifficultyLevel = NormalizeOptionalText(request.DifficultyLevel),
            EstimatedHours = request.EstimatedHours,
            Status = LearningModuleStatusValues.Draft,
            CreatedByUserId = counselorUserId,
            Metadata = "{}",
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.SkillModules.Add(module);
        await _context.SaveChangesAsync(cancellationToken);

        module.Skill = skill;

        return MapModule(module);
    }

    public async Task<SkillModuleDto> UpdateModuleAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        UpdateLearningModuleRequestDto request,
        CancellationToken cancellationToken)
    {
        var module = await GetOwnedModuleQuery(counselorUserId)
            .Include(item => item.Skill)
            .FirstOrDefaultAsync(item => item.SkillModuleId == skillModuleId, cancellationToken);

        if (module == null)
        {
            throw new NotFoundException("Learning module was not found.");
        }

        EnsureEditable(module);

        if (request.SkillId.HasValue && request.SkillId.Value != module.SkillId)
        {
            var skill = await _context.Skills
                .FirstOrDefaultAsync(item => item.SkillId == request.SkillId.Value, cancellationToken);

            if (skill == null)
            {
                throw new NotFoundException("Skill was not found.");
            }

            module.SkillId = skill.SkillId;
            module.Skill = skill;
        }

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            module.Title = request.Title.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Slug))
        {
            module.Slug = await CreateUniqueSlugAsync(
                request.Slug,
                module.SkillModuleId,
                cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(request.Title))
        {
            module.Slug = await CreateUniqueSlugAsync(
                request.Title,
                module.SkillModuleId,
                cancellationToken);
        }

        if (request.Description != null)
        {
            module.Description = NormalizeOptionalText(request.Description);
        }

        if (request.DifficultyLevel != null)
        {
            module.DifficultyLevel = NormalizeOptionalText(request.DifficultyLevel);
        }

        if (request.EstimatedHours.HasValue)
        {
            module.EstimatedHours = request.EstimatedHours;
        }

        if (request.Metadata != null)
        {
            module.Metadata = JsonSerializer.Serialize(request.Metadata, JsonOptions);
        }

        module.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return MapModule(module);
    }

    public async Task DeleteDraftModuleAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        CancellationToken cancellationToken)
    {
        var module = await GetOwnedModuleQuery(counselorUserId)
            .FirstOrDefaultAsync(item => item.SkillModuleId == skillModuleId, cancellationToken);

        if (module == null)
        {
            throw new NotFoundException("Learning module was not found.");
        }

        if (module.Status != LearningModuleStatusValues.Draft)
        {
            throw new ConflictException("Only draft modules can be deleted. Archive published modules instead.");
        }

        _context.SkillModules.Remove(module);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PublishLearningModuleReadinessDto> GetPublishReadinessAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        CancellationToken cancellationToken)
    {
        var module = await GetPublishValidationQuery(counselorUserId)
            .FirstOrDefaultAsync(item => item.SkillModuleId == skillModuleId, cancellationToken);

        if (module == null)
        {
            throw new NotFoundException("Learning module was not found.");
        }

        return ValidatePublishReadiness(module);
    }

    public async Task<PublishLearningModuleResultDto> PublishModuleAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        CancellationToken cancellationToken)
    {
        var module = await GetPublishValidationQuery(counselorUserId)
            .FirstOrDefaultAsync(item => item.SkillModuleId == skillModuleId, cancellationToken);

        if (module == null)
        {
            throw new NotFoundException("Learning module was not found.");
        }

        if (module.Status != LearningModuleStatusValues.Draft)
        {
            throw new ConflictException("Only draft modules can be published.");
        }

        var readiness = ValidatePublishReadiness(module);

        if (!readiness.CanPublish)
        {
            throw new ConflictException(
                "Learning module cannot be published: " + string.Join(" ", readiness.Errors));
        }

        var now = DateTime.UtcNow;

        module.Status = LearningModuleStatusValues.Published;
        module.PublishedAt = now;
        module.ArchivedAt = null;
        module.UpdatedAt = now;

        await _context.SaveChangesAsync(cancellationToken);

        return new PublishLearningModuleResultDto
        {
            SkillModuleId = module.SkillModuleId,
            Status = module.Status,
            PublishedAt = module.PublishedAt ?? now,
            Readiness = readiness
        };
    }

    public async Task<SkillModuleDto> ArchiveModuleAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        CancellationToken cancellationToken)
    {
        var module = await GetOwnedModuleQuery(counselorUserId)
            .Include(item => item.Skill)
            .FirstOrDefaultAsync(item => item.SkillModuleId == skillModuleId, cancellationToken);

        if (module == null)
        {
            throw new NotFoundException("Learning module was not found.");
        }

        if (module.Status != LearningModuleStatusValues.Published)
        {
            throw new ConflictException("Only published modules can be archived.");
        }

        var now = DateTime.UtcNow;

        module.Status = LearningModuleStatusValues.Archived;
        module.ArchivedAt = now;
        module.UpdatedAt = now;

        await _context.SaveChangesAsync(cancellationToken);

        return MapModule(module);
    }

    public async Task<SkillModuleDto> RestoreModuleAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        CancellationToken cancellationToken)
    {
        var module = await GetOwnedModuleQuery(counselorUserId)
            .Include(item => item.Skill)
            .FirstOrDefaultAsync(item => item.SkillModuleId == skillModuleId, cancellationToken);

        if (module == null)
        {
            throw new NotFoundException("Learning module was not found.");
        }

        if (module.Status != LearningModuleStatusValues.Archived)
        {
            throw new ConflictException("Only archived modules can be restored.");
        }

        module.Status = LearningModuleStatusValues.Draft;
        module.ArchivedAt = null;
        module.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return MapModule(module);
    }

    public async Task<LearningModulePreviewDto> GetPreviewAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        CancellationToken cancellationToken)
    {
        var module = await GetOwnedModuleQuery(counselorUserId)
            .Include(item => item.Skill)
            .Include(item => item.SkillModuleLessons)
            .Include(item => item.SkillModuleQuiz)
                .ThenInclude(quiz => quiz!.SkillModuleQuizQuestions)
            .FirstOrDefaultAsync(item => item.SkillModuleId == skillModuleId, cancellationToken);

        if (module == null)
        {
            throw new NotFoundException("Learning module was not found.");
        }

        return MapPreview(module);
    }

    private IQueryable<SkillModule> GetOwnedModuleQuery(Guid counselorUserId)
    {
        return _context.SkillModules
            .Where(module => module.CreatedByUserId == counselorUserId);
    }

    private IQueryable<SkillModule> GetPublishValidationQuery(Guid counselorUserId)
    {
        return GetOwnedModuleQuery(counselorUserId)
            .Include(module => module.Skill)
            .Include(module => module.SkillModuleLessons)
                .ThenInclude(lesson => lesson.SkillModuleChunks)
            .Include(module => module.SkillModuleQuiz)
                .ThenInclude(quiz => quiz!.SkillModuleQuizQuestions)
                    .ThenInclude(question => question.SkillModuleQuizOptions);
    }

    private static void EnsureEditable(SkillModule module)
    {
        if (module.Status != LearningModuleStatusValues.Draft)
        {
            throw new ConflictException("Only draft modules can be edited.");
        }
    }

    private static PublishLearningModuleReadinessDto ValidatePublishReadiness(SkillModule module)
    {
        var errors = new List<string>();

        if (module.SkillId == Guid.Empty)
        {
            errors.Add("Skill is required.");
        }

        if (string.IsNullOrWhiteSpace(module.Title))
        {
            errors.Add("Title is required.");
        }

        if (string.IsNullOrWhiteSpace(module.Slug))
        {
            errors.Add("Slug is required.");
        }

        var lessons = module.SkillModuleLessons
            .OrderBy(lesson => lesson.OrderIndex)
            .ToList();

        if (lessons.Count == 0)
        {
            errors.Add("At least one lesson is required.");
        }

        if (lessons.Any(lesson => lesson.OrderIndex <= 0))
        {
            errors.Add("Lesson order values must be positive.");
        }

        if (lessons.Select(lesson => lesson.OrderIndex).Distinct().Count() != lessons.Count)
        {
            errors.Add("Lesson order values must be unique.");
        }

        if (lessons.Any(lesson => string.IsNullOrWhiteSpace(lesson.MarkdownFileKey)))
        {
            errors.Add("Every lesson must have a Markdown file.");
        }

        if (lessons.Any(lesson => lesson.SkillModuleChunks.Count == 0))
        {
            errors.Add("Every lesson must have generated chunks.");
        }

        var quiz = module.SkillModuleQuiz;

        if (quiz == null)
        {
            errors.Add("Quiz is required.");
        }
        else
        {
            var questions = quiz.SkillModuleQuizQuestions.ToList();

            if (questions.Count == 0)
            {
                errors.Add("Quiz must have at least one question.");
            }

            foreach (var question in questions)
            {
                if (question.SkillModuleQuizOptions.Count < 2)
                {
                    errors.Add("Each quiz question must have at least two options.");
                    break;
                }

                if (question.QuestionType == LearningModuleQuestionTypeValues.SingleChoice
                    && question.SkillModuleQuizOptions.Count(option => option.IsCorrect) != 1)
                {
                    errors.Add("Each single-choice question must have exactly one correct option.");
                    break;
                }
            }
        }

        return new PublishLearningModuleReadinessDto
        {
            CanPublish = errors.Count == 0,
            Errors = errors
        };
    }

    private async Task<string> CreateUniqueSlugAsync(
        string value,
        Guid? excludeModuleId,
        CancellationToken cancellationToken)
    {
        var baseSlug = Slugify(value);

        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            baseSlug = "module";
        }

        var slug = baseSlug;
        var suffix = 2;

        while (await _context.SkillModules.AnyAsync(
            module => module.Slug == slug
                && (!excludeModuleId.HasValue || module.SkillModuleId != excludeModuleId.Value),
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

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static int GetStatusSortOrder(string status)
    {
        return status switch
        {
            LearningModuleStatusValues.Draft => 1,
            LearningModuleStatusValues.Published => 2,
            LearningModuleStatusValues.Archived => 3,
            _ => 4
        };
    }

    private static DateTime GetStatusTime(SkillModule module)
    {
        return module.Status switch
        {
            LearningModuleStatusValues.Published => module.PublishedAt ?? module.UpdatedAt,
            LearningModuleStatusValues.Archived => module.ArchivedAt ?? module.UpdatedAt,
            _ => module.UpdatedAt
        };
    }

    private static SkillModuleDto MapModule(SkillModule module)
    {
        return new SkillModuleDto
        {
            SkillModuleId = module.SkillModuleId,
            SkillId = module.SkillId,
            SkillName = module.Skill?.Name ?? string.Empty,
            SkillSlug = module.Skill?.Slug ?? string.Empty,
            Title = module.Title,
            Slug = module.Slug,
            Description = module.Description,
            DifficultyLevel = module.DifficultyLevel,
            EstimatedHours = module.EstimatedHours,
            Status = module.Status,
            CreatedByUserId = module.CreatedByUserId,
            PublishedAt = module.PublishedAt,
            ArchivedAt = module.ArchivedAt,
            CreatedAt = module.CreatedAt,
            UpdatedAt = module.UpdatedAt
        };
    }

    private static CounselorLearningModuleSummaryDto MapSummary(SkillModule module)
    {
        return new CounselorLearningModuleSummaryDto
        {
            SkillModuleId = module.SkillModuleId,
            SkillId = module.SkillId,
            SkillName = module.Skill.Name,
            SkillSlug = module.Skill.Slug,
            Title = module.Title,
            Slug = module.Slug,
            Description = module.Description,
            DifficultyLevel = module.DifficultyLevel,
            EstimatedHours = module.EstimatedHours,
            Status = module.Status,
            LessonCount = module.SkillModuleLessons.Count,
            HasQuiz = module.SkillModuleQuiz != null,
            QuestionCount = module.SkillModuleQuiz?.SkillModuleQuizQuestions.Count ?? 0,
            PublishedAt = module.PublishedAt,
            ArchivedAt = module.ArchivedAt,
            CreatedAt = module.CreatedAt,
            UpdatedAt = module.UpdatedAt
        };
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

    private static LearningModuleQuizDto MapQuiz(SkillModuleQuiz quiz)
    {
        return new LearningModuleQuizDto
        {
            SkillModuleQuizId = quiz.SkillModuleQuizId,
            SkillModuleId = quiz.SkillModuleId,
            Title = quiz.Title,
            Description = quiz.Description,
            PassingScorePercent = quiz.PassingScorePercent,
            MaxAttempts = quiz.MaxAttempts,
            Status = quiz.Status,
            Questions = quiz.SkillModuleQuizQuestions
                .OrderBy(question => question.OrderIndex)
                .Select(MapQuestion)
                .ToList(),
            CreatedAt = quiz.CreatedAt,
            UpdatedAt = quiz.UpdatedAt
        };
    }

    private static LearningModuleQuizQuestionDto MapQuestion(SkillModuleQuizQuestion question)
    {
        return new LearningModuleQuizQuestionDto
        {
            SkillModuleQuizQuestionId = question.SkillModuleQuizQuestionId,
            SkillModuleQuizId = question.SkillModuleQuizId,
            QuestionText = question.QuestionText,
            QuestionType = question.QuestionType,
            Explanation = question.Explanation,
            OrderIndex = question.OrderIndex,
            Points = question.Points,
            Options = question.SkillModuleQuizOptions
                .OrderBy(option => option.OrderIndex)
                .Select(MapOption)
                .ToList()
        };
    }

    private static LearningModuleQuizOptionDto MapOption(SkillModuleQuizOption option)
    {
        return new LearningModuleQuizOptionDto
        {
            SkillModuleQuizOptionId = option.SkillModuleQuizOptionId,
            SkillModuleQuizQuestionId = option.SkillModuleQuizQuestionId,
            OptionText = option.OptionText,
            IsCorrect = option.IsCorrect,
            Explanation = option.Explanation,
            OrderIndex = option.OrderIndex
        };
    }

    private static LearningModulePreviewDto MapPreview(SkillModule module)
    {
        return new LearningModulePreviewDto
        {
            SkillModuleId = module.SkillModuleId,
            Title = module.Title,
            Slug = module.Slug,
            Description = module.Description,
            DifficultyLevel = module.DifficultyLevel,
            EstimatedHours = module.EstimatedHours,
            SkillName = module.Skill.Name,
            Lessons = module.SkillModuleLessons
                .OrderBy(lesson => lesson.OrderIndex)
                .Select(lesson => new LearningModuleLessonPreviewItemDto
                {
                    SkillModuleLessonId = lesson.SkillModuleLessonId,
                    Title = lesson.Title,
                    Summary = lesson.Summary,
                    OrderIndex = lesson.OrderIndex,
                    EstimatedHours = lesson.EstimatedHours
                })
                .ToList(),
            Quiz = module.SkillModuleQuiz == null
                ? null
                : new LearningModuleQuizPreviewDto
                {
                    SkillModuleQuizId = module.SkillModuleQuiz.SkillModuleQuizId,
                    Title = module.SkillModuleQuiz.Title,
                    Description = module.SkillModuleQuiz.Description,
                    PassingScorePercent = module.SkillModuleQuiz.PassingScorePercent,
                    MaxAttempts = module.SkillModuleQuiz.MaxAttempts,
                    QuestionCount = module.SkillModuleQuiz.SkillModuleQuizQuestions.Count
                }
        };
    }
}
