using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.ContentWorkspace;
using RoadmapPlatform.Application.DTOs.LearningModules;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.LearningModules;
using RoadmapPlatform.Application.Interfaces.Storage;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RoadmapPlatform.Infrastructure.Services.LearningModules;

public sealed class ContentManagerLearningModuleService : IContentManagerLearningModuleService
{
    private const int MinimumLessonCount = 3;
    private const int MinimumQuizQuestionCount = 10;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ApplicationDbContext _context;
    private readonly IFileStorage _fileStorage;

    public ContentManagerLearningModuleService(
        ApplicationDbContext context,
        IFileStorage fileStorage)
    {
        _context = context;
        _fileStorage = fileStorage;
    }


    public async Task<ContentWorkspaceOverviewDto> GetWorkspaceOverviewAsync(
        Guid contentManagerUserId,
        CancellationToken cancellationToken)
    {
        var ownedModules = _context.SkillModules
            .AsNoTracking()
            .Where(module => module.CreatedByUserId == contentManagerUserId);

        var statusRows = await ownedModules
            .GroupBy(module => module.Status)
            .Select(group => new
            {
                Status = group.Key,
                Count = group.Count()
            })
            .ToListAsync(cancellationToken);

        var draftModules = await ownedModules
            .Where(module => module.Status == LearningModuleStatusValues.Draft)
            .Include(module => module.Skill)
            .Include(module => module.SkillModuleLessons)
                .ThenInclude(lesson => lesson.SkillModuleChunks)
            .Include(module => module.SkillModuleQuiz)
                .ThenInclude(quiz => quiz!.SkillModuleQuizQuestions)
                    .ThenInclude(question => question.SkillModuleQuizOptions)
            .OrderByDescending(module => module.UpdatedAt)
            .ToListAsync(cancellationToken);

        var draftReadiness = draftModules
            .Select(module => new
            {
                Module = module,
                Readiness = ValidatePublishReadiness(module)
            })
            .ToList();

        var recentDrafts = draftModules
            .Take(4)
            .Select(MapWorkspaceModule)
            .ToList();

        var readyToPublish = draftReadiness
            .Where(item => item.Readiness.CanPublish)
            .Take(4)
            .Select(item => MapWorkspaceModule(item.Module))
            .ToList();

        var needsAttention = draftReadiness
            .Where(item => !item.Readiness.CanPublish)
            .Take(4)
            .Select(item =>
            {
                var check = item.Readiness.Checks.FirstOrDefault(candidate => !candidate.IsComplete);

                return new ContentWorkspaceAttentionItemDto
                {
                    Module = MapWorkspaceModule(item.Module),
                    CheckKey = check?.Key ?? string.Empty,
                    Label = check?.Label ?? "Needs attention",
                    Message = check?.Description ?? "Review this module."
                };
            })
            .ToList();

        var recentlyPublished = await ownedModules
            .Where(module => module.Status == LearningModuleStatusValues.Published)
            .OrderByDescending(module => module.PublishedAt ?? module.UpdatedAt)
            .Take(4)
            .Select(module => new ContentWorkspaceModuleItemDto
            {
                SkillModuleId = module.SkillModuleId,
                SkillId = module.SkillId,
                SkillName = module.Skill.Name,
                SkillSlug = module.Skill.Slug,
                Title = module.Title,
                Slug = module.Slug,
                Status = module.Status,
                DifficultyLevel = module.DifficultyLevel,
                LessonCount = module.SkillModuleLessons.Count,
                QuestionCount = module.SkillModuleQuiz == null
                    ? 0
                    : module.SkillModuleQuiz.SkillModuleQuizQuestions.Count,
                PublishedAt = module.PublishedAt,
                UpdatedAt = module.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var draftCount = statusRows.FirstOrDefault(item => item.Status == LearningModuleStatusValues.Draft)?.Count ?? 0;
        var publishedCount = statusRows.FirstOrDefault(item => item.Status == LearningModuleStatusValues.Published)?.Count ?? 0;

        return new ContentWorkspaceOverviewDto
        {
            Metrics = new ContentWorkspaceMetricsDto
            {
                Drafts = draftCount,
                ReadyToPublish = draftReadiness.Count(item => item.Readiness.CanPublish),
                NeedsAttention = draftReadiness.Count(item => !item.Readiness.CanPublish),
                Published = publishedCount
            },
            ReadyToPublish = readyToPublish,
            NeedsAttention = needsAttention,
            RecentDrafts = recentDrafts,
            RecentlyPublished = recentlyPublished
        };
    }

    public async Task<ContentManagerLearningModuleListResultDto> GetModulesAsync(
        Guid contentManagerUserId,
        ContentManagerLearningModuleListQueryDto query,
        CancellationToken cancellationToken)
    {
        var status = NormalizeOptionalText(query.Status)?.ToLowerInvariant();
        var sort = NormalizeOptionalText(query.Sort)?.ToLowerInvariant() ?? "updated_desc";
        var safePage = Math.Max(query.Page, 1);
        var safePageSize = Math.Clamp(query.PageSize, 1, 100);

        if (status != null
            && status != LearningModuleStatusValues.Draft
            && status != LearningModuleStatusValues.Published
            && status != LearningModuleStatusValues.Archived)
        {
            throw new ArgumentException("Unsupported learning module status.", nameof(query.Status));
        }

        if (sort is not ("updated_desc" or "created_desc" or "title_asc" or "title_desc"))
        {
            throw new ArgumentException("Unsupported learning module sort value.", nameof(query.Sort));
        }

        var filteredQuery = _context.SkillModules
            .AsNoTracking()
            .Where(module => module.CreatedByUserId == contentManagerUserId);

        var search = NormalizeOptionalText(query.Search);
        if (search != null)
        {
            var pattern = BuildContainsPattern(search);

            filteredQuery = filteredQuery.Where(module =>
                EF.Functions.ILike(module.Title, pattern, "\\")
                || EF.Functions.ILike(module.Slug, pattern, "\\")
                || (module.Description != null
                    && EF.Functions.ILike(module.Description, pattern, "\\"))
                || EF.Functions.ILike(module.Skill.Name, pattern, "\\")
                || EF.Functions.ILike(module.Skill.Slug, pattern, "\\"));
        }

        var difficulty = NormalizeOptionalText(query.Difficulty)?.ToLowerInvariant();
        if (difficulty != null)
        {
            filteredQuery = filteredQuery.Where(module =>
                module.DifficultyLevel != null
                && module.DifficultyLevel.ToLower() == difficulty);
        }

        var statusRows = await filteredQuery
            .GroupBy(module => module.Status)
            .Select(group => new
            {
                Status = group.Key,
                Count = group.Count()
            })
            .ToListAsync(cancellationToken);

        var statusCounts = new ContentManagerLearningModuleStatusCountsDto
        {
            Draft = statusRows.FirstOrDefault(item => item.Status == LearningModuleStatusValues.Draft)?.Count ?? 0,
            Published = statusRows.FirstOrDefault(item => item.Status == LearningModuleStatusValues.Published)?.Count ?? 0,
            Archived = statusRows.FirstOrDefault(item => item.Status == LearningModuleStatusValues.Archived)?.Count ?? 0
        };

        var pageQuery = status == null
            ? filteredQuery
            : filteredQuery.Where(module => module.Status == status);

        var totalCount = await pageQuery.CountAsync(cancellationToken);
        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)safePageSize);
        var effectivePage = totalPages == 0
            ? 1
            : Math.Min(safePage, totalPages);

        var orderedQuery = sort switch
        {
            "created_desc" => pageQuery
                .OrderByDescending(module => module.CreatedAt)
                .ThenBy(module => module.Title),
            "title_asc" => pageQuery
                .OrderBy(module => module.Title)
                .ThenByDescending(module => module.UpdatedAt),
            "title_desc" => pageQuery
                .OrderByDescending(module => module.Title)
                .ThenByDescending(module => module.UpdatedAt),
            _ => pageQuery
                .OrderByDescending(module => module.UpdatedAt)
                .ThenBy(module => module.Title)
        };

        var rows = await orderedQuery
            .Skip((effectivePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(module => new ContentManagerLearningModuleSummaryProjection
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
                QuestionCount = module.SkillModuleQuiz == null
                    ? 0
                    : module.SkillModuleQuiz.SkillModuleQuizQuestions.Count,
                PublishedAt = module.PublishedAt,
                ArchivedAt = module.ArchivedAt,
                CreatedAt = module.CreatedAt,
                UpdatedAt = module.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new ContentManagerLearningModuleListResultDto
        {
            Items = rows.Select(MapSummary).ToList(),
            TotalCount = totalCount,
            Page = effectivePage,
            PageSize = safePageSize,
            TotalPages = totalPages,
            StatusCounts = statusCounts
        };
    }

    public async Task<ContentManagerLearningModuleDetailDto> GetModuleDetailAsync(
        Guid contentManagerUserId,
        Guid skillModuleId,
        CancellationToken cancellationToken)
    {
        var module = await GetOwnedModuleQuery(contentManagerUserId)
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

        return new ContentManagerLearningModuleDetailDto
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
        Guid contentManagerUserId,
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
            CreatedByUserId = contentManagerUserId,
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
        Guid contentManagerUserId,
        Guid skillModuleId,
        UpdateLearningModuleRequestDto request,
        CancellationToken cancellationToken)
    {
        var module = await GetOwnedModuleQuery(contentManagerUserId)
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

        if (request.DescriptionIsSpecified)
        {
            module.Description = NormalizeOptionalText(request.Description);
        }

        if (request.DifficultyLevelIsSpecified)
        {
            module.DifficultyLevel = NormalizeOptionalText(request.DifficultyLevel);
        }

        if (request.EstimatedHoursIsSpecified)
        {
            module.EstimatedHours = request.EstimatedHours;
        }

        if (request.MetadataIsSpecified)
        {
            module.Metadata = request.Metadata == null
                ? "{}"
                : JsonSerializer.Serialize(request.Metadata, JsonOptions);
        }

        module.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return MapModule(module);
    }

    public async Task DeleteDraftModuleAsync(
        Guid contentManagerUserId,
        Guid skillModuleId,
        CancellationToken cancellationToken)
    {
        var module = await GetOwnedModuleQuery(contentManagerUserId)
            .Include(item => item.SkillModuleLessons)
            .FirstOrDefaultAsync(item => item.SkillModuleId == skillModuleId, cancellationToken);

        if (module == null)
        {
            throw new NotFoundException("Learning module was not found.");
        }

        if (module.Status != LearningModuleStatusValues.Draft)
        {
            throw new ConflictException("Only draft modules can be deleted. Archive published modules instead.");
        }

        var lessonFileKeys = module.SkillModuleLessons
            .Select(lesson => lesson.MarkdownFileKey)
            .Where(fileKey => !string.IsNullOrWhiteSpace(fileKey))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        _context.SkillModules.Remove(module);
        await _context.SaveChangesAsync(cancellationToken);

        foreach (var fileKey in lessonFileKeys)
        {
            await TryDeleteStoredFileAsync(fileKey);
        }
    }

    public async Task<PublishLearningModuleReadinessDto> GetPublishReadinessAsync(
        Guid contentManagerUserId,
        Guid skillModuleId,
        CancellationToken cancellationToken)
    {
        var module = await GetPublishValidationQuery(contentManagerUserId)
            .FirstOrDefaultAsync(item => item.SkillModuleId == skillModuleId, cancellationToken);

        if (module == null)
        {
            throw new NotFoundException("Learning module was not found.");
        }

        return ValidatePublishReadiness(module);
    }

    public async Task<PublishLearningModuleResultDto> PublishModuleAsync(
        Guid contentManagerUserId,
        Guid skillModuleId,
        CancellationToken cancellationToken)
    {
        var module = await GetPublishValidationQuery(contentManagerUserId)
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
        Guid contentManagerUserId,
        Guid skillModuleId,
        CancellationToken cancellationToken)
    {
        var module = await GetOwnedModuleQuery(contentManagerUserId)
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

    public async Task<LearningModulePreviewDto> GetPreviewAsync(
        Guid contentManagerUserId,
        Guid skillModuleId,
        CancellationToken cancellationToken)
    {
        var module = await GetOwnedModuleQuery(contentManagerUserId)
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

    private IQueryable<SkillModule> GetOwnedModuleQuery(Guid contentManagerUserId)
    {
        return _context.SkillModules
            .Where(module => module.CreatedByUserId == contentManagerUserId);
    }

    private IQueryable<SkillModule> GetPublishValidationQuery(Guid contentManagerUserId)
    {
        return GetOwnedModuleQuery(contentManagerUserId)
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
        var lessons = module.SkillModuleLessons
            .OrderBy(lesson => lesson.OrderIndex)
            .ToList();

        var overviewMissing = new List<string>();

        if (module.SkillId == Guid.Empty)
        {
            overviewMissing.Add("skill");
        }

        if (string.IsNullOrWhiteSpace(module.Title))
        {
            overviewMissing.Add("title");
        }

        if (string.IsNullOrWhiteSpace(module.Slug))
        {
            overviewMissing.Add("module URL");
        }

        var overviewComplete = overviewMissing.Count == 0;

        var lessonsComplete = lessons.Count >= MinimumLessonCount
            && lessons.All(lesson => lesson.OrderIndex > 0)
            && lessons.Select(lesson => lesson.OrderIndex).Distinct().Count() == lessons.Count
            && lessons.All(lesson => !string.IsNullOrWhiteSpace(lesson.MarkdownFileKey));

        var preparedLessonCount = lessons.Count(lesson =>
            lesson.IndexingStatus == LearningModuleLessonIndexingStatusValues.Indexed
            && lesson.SkillModuleChunks.Count > 0);

        var lessonIndexingComplete = lessons.Count >= MinimumLessonCount
            && preparedLessonCount == lessons.Count;

        var quiz = module.SkillModuleQuiz;
        var questions = quiz?.SkillModuleQuizQuestions.ToList()
            ?? new List<SkillModuleQuizQuestion>();

        var quizComplete = quiz != null
            && questions.Count >= MinimumQuizQuestionCount
            && questions.All(question => question.SkillModuleQuizOptions.Count >= 2)
            && questions
                .Where(question => question.QuestionType == LearningModuleQuestionTypeValues.SingleChoice)
                .All(question => question.SkillModuleQuizOptions.Count(option => option.IsCorrect) == 1);

        var checks = new List<PublishLearningModuleReadinessCheckDto>
        {
            new()
            {
                Key = "overview",
                Label = "Module overview",
                Description = overviewComplete
                    ? "Module details are ready."
                    : $"Add {FormatList(overviewMissing)}.",
                IsComplete = overviewComplete
            },
            new()
            {
                Key = "lessons",
                Label = "Lessons",
                Description = GetLessonReadinessDescription(lessons, lessonsComplete),
                IsComplete = lessonsComplete
            },
            new()
            {
                Key = "lesson_indexing",
                Label = "Learning assistant",
                Description = lessons.Count < MinimumLessonCount
                    ? $"Add at least {MinimumLessonCount} lessons."
                    : lessonIndexingComplete
                        ? "All lessons are ready for the learning assistant."
                        : $"{preparedLessonCount}/{lessons.Count} lessons ready for the learning assistant.",
                IsComplete = lessonIndexingComplete
            },
            new()
            {
                Key = "quiz",
                Label = "Quiz",
                Description = GetQuizReadinessDescription(quiz, questions, quizComplete),
                IsComplete = quizComplete
            }
        };

        var errors = checks
            .Where(check => !check.IsComplete)
            .Select(check => check.Description)
            .ToList();

        return new PublishLearningModuleReadinessDto
        {
            CanPublish = errors.Count == 0,
            Checks = checks,
            Errors = errors
        };
    }

    private static string GetLessonReadinessDescription(
        IReadOnlyCollection<SkillModuleLesson> lessons,
        bool isComplete)
    {
        if (lessons.Count < MinimumLessonCount)
        {
            return $"Add at least {MinimumLessonCount} lessons ({lessons.Count}/{MinimumLessonCount}).";
        }

        if (lessons.Any(lesson => lesson.OrderIndex <= 0)
            || lessons.Select(lesson => lesson.OrderIndex).Distinct().Count() != lessons.Count)
        {
            return "Fix the lesson order.";
        }

        if (lessons.Any(lesson => string.IsNullOrWhiteSpace(lesson.MarkdownFileKey)))
        {
            return "Add content to every lesson.";
        }

        return isComplete
            ? $"{lessons.Count} {Pluralize(lessons.Count, "lesson", "lessons")} ready."
            : "Finish preparing the lessons.";
    }

    private static string GetQuizReadinessDescription(
        SkillModuleQuiz? quiz,
        IReadOnlyCollection<SkillModuleQuizQuestion> questions,
        bool isComplete)
    {
        if (quiz == null)
        {
            return "Create a quiz.";
        }

        if (questions.Count < MinimumQuizQuestionCount)
        {
            return $"Add at least {MinimumQuizQuestionCount} quiz questions ({questions.Count}/{MinimumQuizQuestionCount}).";
        }

        if (questions.Any(question => question.SkillModuleQuizOptions.Count < 2))
        {
            return "Add at least two options to every question.";
        }

        if (questions
            .Where(question => question.QuestionType == LearningModuleQuestionTypeValues.SingleChoice)
            .Any(question => question.SkillModuleQuizOptions.Count(option => option.IsCorrect) != 1))
        {
            return "Set one correct answer for every single-choice question.";
        }

        return isComplete
            ? $"{questions.Count} quiz {Pluralize(questions.Count, "question", "questions")} ready."
            : "Finish the quiz.";
    }

    private static string FormatList(IReadOnlyList<string> values)
    {
        return values.Count switch
        {
            0 => string.Empty,
            1 => values[0],
            2 => $"{values[0]} and {values[1]}",
            _ => $"{string.Join(", ", values.Take(values.Count - 1))}, and {values[^1]}"
        };
    }

    private static string Pluralize(int count, string singular, string plural)
    {
        return count == 1 ? singular : plural;
    }

    private async Task TryDeleteStoredFileAsync(string? fileKey)
    {
        if (string.IsNullOrWhiteSpace(fileKey))
        {
            return;
        }

        try
        {
            await _fileStorage.DeleteAsync(fileKey, CancellationToken.None);
        }
        catch
        {
            // File cleanup is best-effort after the database operation has already succeeded.
        }
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

    private static string BuildContainsPattern(string value)
    {
        var escaped = value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

        return $"%{escaped}%";
    }


    private static ContentWorkspaceModuleItemDto MapWorkspaceModule(SkillModule module)
    {
        return new ContentWorkspaceModuleItemDto
        {
            SkillModuleId = module.SkillModuleId,
            SkillId = module.SkillId,
            SkillName = module.Skill?.Name ?? string.Empty,
            SkillSlug = module.Skill?.Slug ?? string.Empty,
            Title = module.Title,
            Slug = module.Slug,
            Status = module.Status,
            DifficultyLevel = module.DifficultyLevel,
            LessonCount = module.SkillModuleLessons.Count,
            QuestionCount = module.SkillModuleQuiz == null
                ? 0
                : module.SkillModuleQuiz.SkillModuleQuizQuestions.Count,
            PublishedAt = module.PublishedAt,
            UpdatedAt = module.UpdatedAt
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

    private static ContentManagerLearningModuleSummaryDto MapSummary(
        ContentManagerLearningModuleSummaryProjection module)
    {
        return new ContentManagerLearningModuleSummaryDto
        {
            SkillModuleId = module.SkillModuleId,
            SkillId = module.SkillId,
            SkillName = module.SkillName,
            SkillSlug = module.SkillSlug,
            Title = module.Title,
            Slug = module.Slug,
            Description = module.Description,
            DifficultyLevel = module.DifficultyLevel,
            EstimatedHours = module.EstimatedHours,
            Status = module.Status,
            LessonCount = module.LessonCount,
            HasQuiz = module.HasQuiz,
            QuestionCount = module.QuestionCount,
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
            IndexingStatus = lesson.IndexingStatus,
            IndexedAt = lesson.IndexedAt,
            IndexingError = lesson.IndexingError,
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
    private sealed class ContentManagerLearningModuleSummaryProjection
    {
        public Guid SkillModuleId { get; set; }
        public Guid SkillId { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public string SkillSlug { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? DifficultyLevel { get; set; }
        public decimal? EstimatedHours { get; set; }
        public string Status { get; set; } = string.Empty;
        public int LessonCount { get; set; }
        public int QuestionCount { get; set; }
        public bool HasQuiz { get; set; }
        public DateTime? PublishedAt { get; set; }
        public DateTime? ArchivedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

}
