using Microsoft.EntityFrameworkCore;
using Npgsql;
using RoadmapPlatform.Application.DTOs.LearningModules;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.LearningModules;
using RoadmapPlatform.Application.Interfaces.Storage;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services.Users;
using System.Text;
using System.Text.Json;

namespace RoadmapPlatform.Infrastructure.Services.LearningModules;

public sealed class LearnerLearningModuleService : ILearnerLearningModuleService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ApplicationDbContext _context;
    private readonly IFileStorage _fileStorage;

    public LearnerLearningModuleService(
        ApplicationDbContext context,
        IFileStorage fileStorage)
    {
        _context = context;
        _fileStorage = fileStorage;
    }

    public async Task<IReadOnlyList<LearnerLearningModuleSummaryDto>> GetPublishedModulesAsync(
        CancellationToken cancellationToken)
    {
        var modules = await _context.SkillModules
            .AsNoTracking()
            .Include(module => module.Skill)
            .Include(module => module.CreatedByUser)
                .ThenInclude(user => user!.UserProfile)
            .Include(module => module.SkillModuleLessons)
            .Include(module => module.SkillModuleQuiz)
                .ThenInclude(quiz => quiz!.SkillModuleQuizQuestions)
            .Where(module => module.Status == LearningModuleStatusValues.Published)
            .OrderByDescending(module => module.PublishedAt)
            .ThenBy(module => module.Title)
            .ToListAsync(cancellationToken);

        return modules
            .Select(module => MapLearnerSummary(module, enrollment: null))
            .ToList();
    }

    public async Task<IReadOnlyList<LearnerLearningModuleSummaryDto>> GetEnrolledModulesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var modules = await _context.SkillModules
            .AsNoTracking()
            .Include(module => module.Skill)
            .Include(module => module.CreatedByUser)
                .ThenInclude(user => user!.UserProfile)
            .Include(module => module.SkillModuleLessons)
            .Include(module => module.SkillModuleQuiz)
                .ThenInclude(quiz => quiz!.SkillModuleQuizQuestions)
            .Where(module =>
                (
                    module.Status == LearningModuleStatusValues.Published
                    || module.Status == LearningModuleStatusValues.Archived)
                && module.SkillModuleEnrollments.Any(enrollment => enrollment.UserId == userId))
            .OrderBy(module => module.Status == LearningModuleStatusValues.Archived)
            .ThenByDescending(module => module.PublishedAt)
            .ThenBy(module => module.Title)
            .ToListAsync(cancellationToken);

        var enrollments = await GetEnrollmentsByModuleIdAsync(
            userId,
            modules.Select(module => module.SkillModuleId).ToList(),
            cancellationToken);

        return modules
            .Select(module => MapLearnerSummary(
                module,
                enrollments.GetValueOrDefault(module.SkillModuleId)))
            .ToList();
    }

    public async Task<LearnerLearningModuleOverviewDto> GetPublishedModuleBySlugAsync(
        string slug,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var module = await _context.SkillModules
            .AsNoTracking()
            .Include(item => item.Skill)
            .Include(item => item.CreatedByUser)
                .ThenInclude(user => user!.UserProfile)
            .Include(item => item.SkillModuleLessons)
            .Include(item => item.SkillModuleQuiz)
                .ThenInclude(quiz => quiz!.SkillModuleQuizQuestions)
            .FirstOrDefaultAsync(item =>
                item.Slug == slug
                && (
                    item.Status == LearningModuleStatusValues.Published
                    || item.Status == LearningModuleStatusValues.Archived),
                cancellationToken);

        if (module == null)
        {
            throw new NotFoundException("Learning module was not found.");
        }

        var enrollment = userId.HasValue
            ? await _context.SkillModuleEnrollments
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.UserId == userId.Value
                    && item.SkillModuleId == module.SkillModuleId,
                    cancellationToken)
            : null;

        EnsureLearnerCanAccessModule(module, enrollment);

        return MapLearnerOverview(module, enrollment);
    }

    public async Task<LearningModuleEnrollmentDto> EnrollAsync(
        Guid userId,
        Guid skillModuleId,
        CancellationToken cancellationToken)
    {
        var moduleExists = await _context.SkillModules
            .AsNoTracking()
            .AnyAsync(item =>
                item.SkillModuleId == skillModuleId
                && item.Status == LearningModuleStatusValues.Published,
                cancellationToken);

        if (!moduleExists)
        {
            throw new NotFoundException("Learning module was not found.");
        }

        var existingEnrollment = await _context.SkillModuleEnrollments
            .FirstOrDefaultAsync(item =>
                item.UserId == userId
                && item.SkillModuleId == skillModuleId,
                cancellationToken);

        if (existingEnrollment != null)
        {
            return MapEnrollment(existingEnrollment);
        }

        var now = DateTime.UtcNow;

        var enrollment = new SkillModuleEnrollment
        {
            SkillModuleEnrollmentId = Guid.NewGuid(),
            UserId = userId,
            SkillModuleId = skillModuleId,
            Status = LearningModuleEnrollmentStatusValues.InProgress,
            StartedAt = now,
            CompletedAt = null,
            LastAccessedLessonId = null,
            ProgressPercent = 0,
            LessonProgress = "{}",
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.SkillModuleEnrollments.Add(enrollment);
        await _context.SaveChangesAsync(cancellationToken);

        return MapEnrollment(enrollment);
    }

    public async Task<LearningModuleLessonContentDto> GetLessonContentAsync(
        Guid userId,
        Guid skillModuleId,
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        await EnsureEnrollmentExistsAsync(
            userId,
            skillModuleId,
            cancellationToken);

        var lesson = await _context.SkillModuleLessons
            .AsNoTracking()
            .Include(item => item.SkillModule)
            .FirstOrDefaultAsync(item =>
                item.SkillModuleLessonId == lessonId
                && item.SkillModuleId == skillModuleId
                && (item.SkillModule.Status == LearningModuleStatusValues.Published
                    || item.SkillModule.Status == LearningModuleStatusValues.Archived),
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

    public async Task<UpdateLessonProgressResultDto> UpdateLessonProgressAsync(
        Guid userId,
        Guid skillModuleId,
        Guid lessonId,
        UpdateLessonProgressRequestDto request,
        CancellationToken cancellationToken)
    {
        var status = NormalizeLessonProgressStatus(request.Status);

        var module = await _context.SkillModules
            .Include(item => item.SkillModuleLessons)
            .Include(item => item.SkillModuleQuiz)
            .FirstOrDefaultAsync(item =>
                item.SkillModuleId == skillModuleId
                && (
                    item.Status == LearningModuleStatusValues.Published
                    || item.Status == LearningModuleStatusValues.Archived),
                cancellationToken);

        if (module == null)
        {
            throw new NotFoundException("Learning module was not found.");
        }

        var lessonExists = module.SkillModuleLessons
            .Any(lesson => lesson.SkillModuleLessonId == lessonId);

        if (!lessonExists)
        {
            throw new NotFoundException("Learning module lesson was not found.");
        }

        var enrollment = await _context.SkillModuleEnrollments
            .FirstOrDefaultAsync(item =>
                item.UserId == userId
                && item.SkillModuleId == skillModuleId,
                cancellationToken);

        if (enrollment == null)
        {
            throw new ConflictException("Start the module before updating lesson progress.");
        }

        var lessonProgress = ParseLessonProgress(enrollment.LessonProgress);
        lessonProgress[lessonId] = status;

        var now = DateTime.UtcNow;
        var quizPassed = module.SkillModuleQuiz != null
            && await HasPassedQuizAsync(
                userId,
                module.SkillModuleQuiz.SkillModuleQuizId,
                cancellationToken);

        enrollment.LessonProgress = SerializeLessonProgress(lessonProgress);
        enrollment.LastAccessedLessonId = lessonId;

        ApplyEnrollmentProgress(
            enrollment,
            module.SkillModuleLessons,
            lessonProgress,
            hasQuiz: module.SkillModuleQuiz != null,
            quizPassed,
            now);

        await _context.SaveChangesAsync(cancellationToken);

        return new UpdateLessonProgressResultDto
        {
            SkillModuleEnrollmentId = enrollment.SkillModuleEnrollmentId,
            SkillModuleLessonId = lessonId,
            LessonStatus = status,
            ProgressPercent = enrollment.ProgressPercent,
            EnrollmentStatus = enrollment.Status,
            UpdatedAt = enrollment.UpdatedAt
        };
    }


    public async Task<IReadOnlyList<QuizAttemptSummaryDto>> GetQuizAttemptsAsync(
        Guid userId,
        Guid skillModuleId,
        CancellationToken cancellationToken)
    {
        await EnsureEnrollmentExistsAsync(
            userId,
            skillModuleId,
            cancellationToken);

        var quiz = await _context.SkillModuleQuizzes
            .AsNoTracking()
            .Include(item => item.SkillModule)
            .FirstOrDefaultAsync(item =>
                item.SkillModuleId == skillModuleId
                && (item.SkillModule.Status == LearningModuleStatusValues.Published
                    || item.SkillModule.Status == LearningModuleStatusValues.Archived),
                cancellationToken);

        if (quiz == null)
        {
            throw new NotFoundException("Quiz was not found.");
        }

        return await _context.SkillModuleQuizAttempts
            .AsNoTracking()
            .Where(item =>
                item.SkillModuleQuizId == quiz.SkillModuleQuizId
                && item.UserId == userId)
            .OrderByDescending(item => item.AttemptNo)
            .Select(item => new QuizAttemptSummaryDto
            {
                SkillModuleQuizAttemptId = item.SkillModuleQuizAttemptId,
                SkillModuleQuizId = item.SkillModuleQuizId,
                AttemptNo = item.AttemptNo,
                Status = item.Status,
                StartedAt = item.StartedAt,
                SubmittedAt = item.SubmittedAt,
                ScorePercent = item.ScorePercent,
                EarnedPoints = item.EarnedPoints,
                TotalPoints = item.TotalPoints,
                Passed = item.Passed
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<StartQuizAttemptResultDto> StartQuizAttemptAsync(
        Guid userId,
        Guid skillModuleId,
        CancellationToken cancellationToken)
    {
        var enrollment = await EnsureEnrollmentExistsAsync(
            userId,
            skillModuleId,
            cancellationToken);

        var quiz = await _context.SkillModuleQuizzes
            .Include(item => item.SkillModule)
                .ThenInclude(module => module.SkillModuleLessons)
            .Include(item => item.SkillModuleQuizQuestions)
                .ThenInclude(question => question.SkillModuleQuizOptions)
            .FirstOrDefaultAsync(item =>
                item.SkillModuleId == skillModuleId
                && (item.SkillModule.Status == LearningModuleStatusValues.Published
                    || item.SkillModule.Status == LearningModuleStatusValues.Archived),
                cancellationToken);

        if (quiz == null)
        {
            throw new NotFoundException("Quiz was not found.");
        }

        EnsureAllLessonsCompletedBeforeQuiz(
            enrollment,
            quiz.SkillModule.SkillModuleLessons);

        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable,
                cancellationToken);

            var existingInProgressAttempt = await GetExistingInProgressAttemptAsync(
                quiz.SkillModuleQuizId,
                userId,
                cancellationToken);

            if (existingInProgressAttempt != null)
            {
                await transaction.CommitAsync(cancellationToken);
                return MapStartQuizAttemptResult(existingInProgressAttempt, quiz);
            }

            var now = DateTime.UtcNow;
            var attemptDayStart = now.Date;
            var attemptDayEnd = attemptDayStart.AddDays(1);

            var submittedAttemptsToday = await _context.SkillModuleQuizAttempts
                .CountAsync(item =>
                    item.SkillModuleQuizId == quiz.SkillModuleQuizId
                    && item.UserId == userId
                    && item.Status == LearningModuleQuizAttemptStatusValues.Submitted
                    && item.SubmittedAt >= attemptDayStart
                    && item.SubmittedAt < attemptDayEnd,
                    cancellationToken);

            if (quiz.MaxAttempts.HasValue && submittedAttemptsToday >= quiz.MaxAttempts.Value)
            {
                throw new ConflictException("Maximum quiz attempts for today reached. Try again tomorrow.");
            }

            var lastAttemptNo = await _context.SkillModuleQuizAttempts
                .Where(item =>
                    item.SkillModuleQuizId == quiz.SkillModuleQuizId
                    && item.UserId == userId)
                .Select(item => (int?)item.AttemptNo)
                .MaxAsync(cancellationToken);

            var attempt = new SkillModuleQuizAttempt
            {
                SkillModuleQuizAttemptId = Guid.NewGuid(),
                SkillModuleQuizId = quiz.SkillModuleQuizId,
                SkillModuleEnrollmentId = enrollment.SkillModuleEnrollmentId,
                UserId = userId,
                AttemptNo = (lastAttemptNo ?? 0) + 1,
                Status = LearningModuleQuizAttemptStatusValues.InProgress,
                StartedAt = now,
                SubmittedAt = null,
                ScorePercent = null,
                EarnedPoints = null,
                TotalPoints = null,
                Passed = null
            };

            _context.SkillModuleQuizAttempts.Add(attempt);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return MapStartQuizAttemptResult(attempt, quiz);
        }
        catch (DbUpdateException ex) when (IsQuizRequestConflict(ex))
        {
            _context.ChangeTracker.Clear();

            var existingInProgressAttempt = await GetExistingInProgressAttemptAsync(
                quiz.SkillModuleQuizId,
                userId,
                cancellationToken);

            if (existingInProgressAttempt != null)
            {
                return MapStartQuizAttemptResult(existingInProgressAttempt, quiz);
            }

            throw new ConflictException("A quiz attempt was already started. Refresh and try again.");
        }
        catch (PostgresException ex) when (IsQuizRequestConflict(ex))
        {
            _context.ChangeTracker.Clear();

            var existingInProgressAttempt = await GetExistingInProgressAttemptAsync(
                quiz.SkillModuleQuizId,
                userId,
                cancellationToken);

            if (existingInProgressAttempt != null)
            {
                return MapStartQuizAttemptResult(existingInProgressAttempt, quiz);
            }

            throw new ConflictException("A quiz attempt was already started. Refresh and try again.");
        }
    }

    public async Task<StartQuizAttemptResultDto> GetQuizAttemptSessionAsync(
        Guid userId,
        Guid skillModuleId,
        Guid attemptId,
        CancellationToken cancellationToken)
    {
        var attempt = await _context.SkillModuleQuizAttempts
            .AsNoTracking()
            .Include(item => item.SkillModuleQuiz)
                .ThenInclude(quiz => quiz.SkillModule)
            .Include(item => item.SkillModuleQuiz)
                .ThenInclude(quiz => quiz.SkillModuleQuizQuestions)
                    .ThenInclude(question => question.SkillModuleQuizOptions)
            .FirstOrDefaultAsync(item =>
                item.SkillModuleQuizAttemptId == attemptId
                && item.UserId == userId
                && item.SkillModuleQuiz.SkillModuleId == skillModuleId
                && (item.SkillModuleQuiz.SkillModule.Status == LearningModuleStatusValues.Published
                    || item.SkillModuleQuiz.SkillModule.Status == LearningModuleStatusValues.Archived),
                cancellationToken);

        if (attempt == null)
        {
            throw new NotFoundException("Quiz attempt was not found.");
        }

        if (attempt.Status != LearningModuleQuizAttemptStatusValues.InProgress)
        {
            throw new ConflictException("This quiz attempt has already been submitted.");
        }

        return MapStartQuizAttemptResult(attempt, attempt.SkillModuleQuiz);
    }

    public async Task<QuizAttemptReviewDto> SubmitQuizAttemptAsync(
        Guid userId,
        Guid skillModuleId,
        Guid attemptId,
        SubmitQuizAttemptRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable,
                cancellationToken);

            var attempt = await _context.SkillModuleQuizAttempts
                .Include(item => item.SkillModuleEnrollment)
                .Include(item => item.SkillModuleQuiz)
                    .ThenInclude(quiz => quiz.SkillModule)
                        .ThenInclude(module => module.SkillModuleLessons)
                .Include(item => item.SkillModuleQuiz)
                    .ThenInclude(quiz => quiz.SkillModuleQuizQuestions)
                        .ThenInclude(question => question.SkillModuleQuizOptions)
                .Include(item => item.SkillModuleQuizAnswers)
                .FirstOrDefaultAsync(item =>
                    item.SkillModuleQuizAttemptId == attemptId
                    && item.UserId == userId
                    && item.SkillModuleQuiz.SkillModuleId == skillModuleId
                    && (item.SkillModuleQuiz.SkillModule.Status == LearningModuleStatusValues.Published
                        || item.SkillModuleQuiz.SkillModule.Status == LearningModuleStatusValues.Archived),
                    cancellationToken);

            if (attempt == null)
            {
                throw new NotFoundException("Quiz attempt was not found.");
            }

            if (attempt.Status != LearningModuleQuizAttemptStatusValues.InProgress)
            {
                throw new ConflictException("This quiz attempt has already been submitted.");
            }

            if (attempt.SkillModuleQuizAnswers.Count > 0)
            {
                throw new ConflictException("This quiz attempt has already been submitted.");
            }

            var questions = attempt.SkillModuleQuiz.SkillModuleQuizQuestions
                .OrderBy(question => question.OrderIndex)
                .ToList();

            ValidateSubmitRequest(request, questions);

            var selectedOptionByQuestionId = request.Answers.ToDictionary(
                answer => answer.SkillModuleQuizQuestionId,
                answer => answer.SelectedOptionId);

            var answers = new List<SkillModuleQuizAnswer>();
            var earnedPoints = 0;
            var totalPoints = questions.Sum(question => question.Points);
            var now = DateTime.UtcNow;

            foreach (var question in questions)
            {
                var selectedOptionId = selectedOptionByQuestionId[question.SkillModuleQuizQuestionId];

                var selectedOption = question.SkillModuleQuizOptions
                    .First(option => option.SkillModuleQuizOptionId == selectedOptionId);

                var isCorrect = selectedOption.IsCorrect;
                var questionEarnedPoints = isCorrect ? question.Points : 0;

                earnedPoints += questionEarnedPoints;

                answers.Add(new SkillModuleQuizAnswer
                {
                    SkillModuleQuizAnswerId = Guid.NewGuid(),
                    SkillModuleQuizAttemptId = attempt.SkillModuleQuizAttemptId,
                    SkillModuleQuizQuestionId = question.SkillModuleQuizQuestionId,
                    SelectedOptionId = selectedOption.SkillModuleQuizOptionId,
                    IsCorrect = isCorrect,
                    EarnedPoints = questionEarnedPoints,
                    AnsweredAt = now
                });
            }

            var scorePercent = totalPoints == 0
                ? 0
                : Math.Round((decimal)earnedPoints * 100 / totalPoints, 2);

            attempt.Status = LearningModuleQuizAttemptStatusValues.Submitted;
            attempt.SubmittedAt = now;
            attempt.ScorePercent = scorePercent;
            attempt.EarnedPoints = earnedPoints;
            attempt.TotalPoints = totalPoints;
            attempt.Passed = scorePercent >= attempt.SkillModuleQuiz.PassingScorePercent;

            var lessonProgress = ParseLessonProgress(attempt.SkillModuleEnrollment.LessonProgress);

            ApplyEnrollmentProgress(
                attempt.SkillModuleEnrollment,
                attempt.SkillModuleQuiz.SkillModule.SkillModuleLessons,
                lessonProgress,
                hasQuiz: true,
                quizPassed: attempt.Passed == true,
                now);

            _context.SkillModuleQuizAnswers.AddRange(answers);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsQuizRequestConflict(ex))
        {
            _context.ChangeTracker.Clear();
            throw new ConflictException("This quiz attempt has already been submitted.");
        }
        catch (PostgresException ex) when (IsQuizRequestConflict(ex))
        {
            _context.ChangeTracker.Clear();
            throw new ConflictException("This quiz attempt has already been submitted.");
        }

        return await GetQuizAttemptReviewAsync(
            userId,
            skillModuleId,
            attemptId,
            cancellationToken);
    }

    public async Task<QuizAttemptReviewDto> GetQuizAttemptReviewAsync(
        Guid userId,
        Guid skillModuleId,
        Guid attemptId,
        CancellationToken cancellationToken)
    {
        var attempt = await _context.SkillModuleQuizAttempts
            .AsNoTracking()
            .Include(item => item.SkillModuleQuiz)
                .ThenInclude(quiz => quiz.SkillModule)
            .Include(item => item.SkillModuleQuiz)
                .ThenInclude(quiz => quiz.SkillModuleQuizQuestions)
                    .ThenInclude(question => question.SkillModuleQuizOptions)
            .Include(item => item.SkillModuleQuizAnswers)
            .FirstOrDefaultAsync(item =>
                item.SkillModuleQuizAttemptId == attemptId
                && item.UserId == userId
                && item.SkillModuleQuiz.SkillModuleId == skillModuleId
                && (item.SkillModuleQuiz.SkillModule.Status == LearningModuleStatusValues.Published
                    || item.SkillModuleQuiz.SkillModule.Status == LearningModuleStatusValues.Archived),
                cancellationToken);

        if (attempt == null)
        {
            throw new NotFoundException("Quiz attempt was not found.");
        }

        if (attempt.Status != LearningModuleQuizAttemptStatusValues.Submitted)
        {
            throw new ConflictException("Quiz attempt is still in progress.");
        }

        return MapAttemptReview(attempt);
    }

    private async Task<SkillModuleQuizAttempt?> GetExistingInProgressAttemptAsync(
        Guid quizId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _context.SkillModuleQuizAttempts
            .FirstOrDefaultAsync(item =>
                item.SkillModuleQuizId == quizId
                && item.UserId == userId
                && item.Status == LearningModuleQuizAttemptStatusValues.InProgress,
                cancellationToken);
    }

    private static StartQuizAttemptResultDto MapStartQuizAttemptResult(
        SkillModuleQuizAttempt attempt,
        SkillModuleQuiz quiz)
    {
        return new StartQuizAttemptResultDto
        {
            SkillModuleQuizAttemptId = attempt.SkillModuleQuizAttemptId,
            SkillModuleQuizId = attempt.SkillModuleQuizId,
            AttemptNo = attempt.AttemptNo,
            Status = attempt.Status,
            StartedAt = attempt.StartedAt,
            Quiz = MapQuizForLearnerAttempt(quiz)
        };
    }

    private static bool IsQuizRequestConflict(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException
            && IsQuizRequestConflict(postgresException);
    }

    private static bool IsQuizRequestConflict(PostgresException exception)
    {
        return exception.SqlState == PostgresErrorCodes.UniqueViolation
            || exception.SqlState == PostgresErrorCodes.SerializationFailure;
    }

    private static void EnsureLearnerCanAccessModule(
        SkillModule module,
        SkillModuleEnrollment? enrollment)
    {
        if (module.Status == LearningModuleStatusValues.Published)
        {
            return;
        }

        if (module.Status == LearningModuleStatusValues.Archived && enrollment != null)
        {
            return;
        }

        throw new NotFoundException("Learning module was not found.");
    }

    private async Task<SkillModuleEnrollment> EnsureEnrollmentExistsAsync(
        Guid userId,
        Guid skillModuleId,
        CancellationToken cancellationToken)
    {
        var enrollment = await _context.SkillModuleEnrollments
            .FirstOrDefaultAsync(item =>
                item.UserId == userId
                && item.SkillModuleId == skillModuleId,
                cancellationToken);

        if (enrollment == null)
        {
            throw new ConflictException("Start the module first.");
        }

        return enrollment;
    }

    private async Task<Dictionary<Guid, SkillModuleEnrollment>> GetEnrollmentsByModuleIdAsync(
        Guid? userId,
        IReadOnlyList<Guid> moduleIds,
        CancellationToken cancellationToken)
    {
        if (!userId.HasValue || moduleIds.Count == 0)
        {
            return [];
        }

        return await _context.SkillModuleEnrollments
            .AsNoTracking()
            .Where(item =>
                item.UserId == userId.Value
                && moduleIds.Contains(item.SkillModuleId))
            .ToDictionaryAsync(
                item => item.SkillModuleId,
                item => item,
                cancellationToken);
    }

    private async Task<bool> HasPassedQuizAsync(
        Guid userId,
        Guid quizId,
        CancellationToken cancellationToken)
    {
        return await _context.SkillModuleQuizAttempts
            .AsNoTracking()
            .AnyAsync(item =>
                item.UserId == userId
                && item.SkillModuleQuizId == quizId
                && item.Status == LearningModuleQuizAttemptStatusValues.Submitted
                && item.Passed == true,
                cancellationToken);
    }

    private static void ApplyEnrollmentProgress(
        SkillModuleEnrollment enrollment,
        IEnumerable<SkillModuleLesson> lessons,
        IReadOnlyDictionary<Guid, string> lessonProgress,
        bool hasQuiz,
        bool quizPassed,
        DateTime now)
    {
        var progressPercent = CalculateModuleProgress(
            lessons,
            lessonProgress,
            hasQuiz,
            quizPassed);

        enrollment.ProgressPercent = progressPercent;
        enrollment.UpdatedAt = now;

        if (progressPercent >= 100)
        {
            enrollment.Status = LearningModuleEnrollmentStatusValues.Completed;
            enrollment.CompletedAt ??= now;
        }
        else
        {
            enrollment.Status = LearningModuleEnrollmentStatusValues.InProgress;
            enrollment.CompletedAt = null;
        }
    }

    private static decimal CalculateModuleProgress(
        IEnumerable<SkillModuleLesson> lessons,
        IReadOnlyDictionary<Guid, string> lessonProgress,
        bool hasQuiz,
        bool quizPassed)
    {
        var lessonList = lessons.ToList();
        var totalUnits = lessonList.Count + (hasQuiz ? 1 : 0);

        if (totalUnits == 0)
        {
            return 0;
        }

        var completedLessonUnits = lessonList.Count(lesson =>
            lessonProgress.TryGetValue(lesson.SkillModuleLessonId, out var lessonStatus)
            && lessonStatus == LearningModuleLessonProgressStatusValues.Completed);

        var completedUnits = completedLessonUnits + (hasQuiz && quizPassed ? 1 : 0);

        return Math.Round((decimal)completedUnits * 100 / totalUnits, 2);
    }

    private static void EnsureAllLessonsCompletedBeforeQuiz(
        SkillModuleEnrollment enrollment,
        IEnumerable<SkillModuleLesson> lessons)
    {
        var lessonProgress = ParseLessonProgress(enrollment.LessonProgress);
        var incompleteLessonExists = lessons.Any(lesson =>
            !lessonProgress.TryGetValue(lesson.SkillModuleLessonId, out var status)
            || status != LearningModuleLessonProgressStatusValues.Completed);

        if (incompleteLessonExists)
        {
            throw new ConflictException("Complete all lessons before starting the quiz.");
        }
    }

    private static void ValidateSubmitRequest(
        SubmitQuizAttemptRequestDto request,
        IReadOnlyList<SkillModuleQuizQuestion> questions)
    {
        if (request.Answers.Count == 0)
        {
            throw new ConflictException("At least one answer is required.");
        }

        var questionIds = questions
            .Select(question => question.SkillModuleQuizQuestionId)
            .OrderBy(id => id)
            .ToList();

        var answerQuestionIds = request.Answers
            .Select(answer => answer.SkillModuleQuizQuestionId)
            .OrderBy(id => id)
            .ToList();

        if (!questionIds.SequenceEqual(answerQuestionIds))
        {
            throw new ConflictException("Submit request must include one answer for every quiz question.");
        }

        if (request.Answers
            .Select(answer => answer.SkillModuleQuizQuestionId)
            .Distinct()
            .Count() != request.Answers.Count)
        {
            throw new ConflictException("Each question can only be answered once.");
        }

        foreach (var answer in request.Answers)
        {
            var question = questions.First(item =>
                item.SkillModuleQuizQuestionId == answer.SkillModuleQuizQuestionId);

            var optionBelongsToQuestion = question.SkillModuleQuizOptions
                .Any(option => option.SkillModuleQuizOptionId == answer.SelectedOptionId);

            if (!optionBelongsToQuestion)
            {
                throw new ConflictException("Selected option does not belong to the submitted question.");
            }
        }
    }

    private static string NormalizeLessonProgressStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ConflictException("Lesson progress status is required.");
        }

        var normalized = status.Trim().ToLowerInvariant();

        return normalized switch
        {
            LearningModuleLessonProgressStatusValues.InProgress => normalized,
            LearningModuleLessonProgressStatusValues.Completed => normalized,
            _ => throw new ConflictException("Invalid lesson progress status.")
        };
    }

    private static Dictionary<Guid, string> ParseLessonProgress(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions)
            ?? [];

        var result = new Dictionary<Guid, string>();

        foreach (var item in raw)
        {
            if (Guid.TryParse(item.Key, out var lessonId))
            {
                result[lessonId] = item.Value;
            }
        }

        return result;
    }

    private static string SerializeLessonProgress(Dictionary<Guid, string> lessonProgress)
    {
        var raw = lessonProgress.ToDictionary(
            item => item.Key.ToString(),
            item => item.Value);

        return JsonSerializer.Serialize(raw, JsonOptions);
    }

    private static LearnerLearningModuleSummaryDto MapLearnerSummary(
        SkillModule module,
        SkillModuleEnrollment? enrollment)
    {
        return new LearnerLearningModuleSummaryDto
        {
            CreatorProfile = CreatorProfileMapper.Map(module.CreatedByUser),
            SkillModuleId = module.SkillModuleId,
            SkillId = module.SkillId,
            SkillName = module.Skill.Name,
            Title = module.Title,
            Slug = module.Slug,
            Status = module.Status,
            Description = module.Description,
            DifficultyLevel = module.DifficultyLevel,
            EstimatedHours = module.EstimatedHours,
            LessonCount = module.SkillModuleLessons.Count,
            QuestionCount = module.SkillModuleQuiz?.SkillModuleQuizQuestions.Count ?? 0,
            Enrollment = enrollment == null ? null : MapEnrollment(enrollment)
        };
    }

    private static LearnerLearningModuleOverviewDto MapLearnerOverview(
        SkillModule module,
        SkillModuleEnrollment? enrollment)
    {
        return new LearnerLearningModuleOverviewDto
        {
            CreatorProfile = CreatorProfileMapper.Map(module.CreatedByUser),
            SkillModuleId = module.SkillModuleId,
            SkillId = module.SkillId,
            SkillName = module.Skill.Name,
            Title = module.Title,
            Slug = module.Slug,
            Status = module.Status,
            Description = module.Description,
            DifficultyLevel = module.DifficultyLevel,
            EstimatedHours = module.EstimatedHours,
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
                    QuestionCount = module.SkillModuleQuiz.SkillModuleQuizQuestions.Count,
                    PassingScorePercent = module.SkillModuleQuiz.PassingScorePercent,
                    MaxAttempts = module.SkillModuleQuiz.MaxAttempts
                },
            Enrollment = enrollment == null ? null : MapEnrollment(enrollment)
        };
    }

    private static LearningModuleEnrollmentDto MapEnrollment(SkillModuleEnrollment enrollment)
    {
        return new LearningModuleEnrollmentDto
        {
            SkillModuleEnrollmentId = enrollment.SkillModuleEnrollmentId,
            UserId = enrollment.UserId,
            SkillModuleId = enrollment.SkillModuleId,
            Status = enrollment.Status,
            StartedAt = enrollment.StartedAt,
            CompletedAt = enrollment.CompletedAt,
            LastAccessedLessonId = enrollment.LastAccessedLessonId,
            ProgressPercent = enrollment.ProgressPercent,
            LessonProgress = ParseLessonProgress(enrollment.LessonProgress)
        };
    }

    private static LearningModuleQuizDto MapQuizForLearnerAttempt(SkillModuleQuiz quiz)
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
                .Select(question => new LearningModuleQuizQuestionDto
                {
                    SkillModuleQuizQuestionId = question.SkillModuleQuizQuestionId,
                    SkillModuleQuizId = question.SkillModuleQuizId,
                    QuestionText = question.QuestionText,
                    QuestionType = question.QuestionType,
                    Explanation = null,
                    OrderIndex = question.OrderIndex,
                    Points = question.Points,
                    Options = question.SkillModuleQuizOptions
                        .OrderBy(option => option.OrderIndex)
                        .Select(option => new LearningModuleQuizOptionDto
                        {
                            SkillModuleQuizOptionId = option.SkillModuleQuizOptionId,
                            SkillModuleQuizQuestionId = option.SkillModuleQuizQuestionId,
                            OptionText = option.OptionText,
                            IsCorrect = false,
                            Explanation = null,
                            OrderIndex = option.OrderIndex
                        })
                        .ToList()
                })
                .ToList(),
            CreatedAt = quiz.CreatedAt,
            UpdatedAt = quiz.UpdatedAt
        };
    }

    private static QuizAttemptReviewDto MapAttemptReview(SkillModuleQuizAttempt attempt)
    {
        var questions = attempt.SkillModuleQuiz.SkillModuleQuizQuestions
            .OrderBy(question => question.OrderIndex)
            .ToList();

        var answersByQuestionId = attempt.SkillModuleQuizAnswers
            .ToDictionary(answer => answer.SkillModuleQuizQuestionId);

        return new QuizAttemptReviewDto
        {
            SkillModuleQuizAttemptId = attempt.SkillModuleQuizAttemptId,
            SkillModuleQuizId = attempt.SkillModuleQuizId,
            SkillModuleEnrollmentId = attempt.SkillModuleEnrollmentId,
            UserId = attempt.UserId,
            AttemptNo = attempt.AttemptNo,
            Status = attempt.Status,
            StartedAt = attempt.StartedAt,
            SubmittedAt = attempt.SubmittedAt,
            ScorePercent = attempt.ScorePercent,
            EarnedPoints = attempt.EarnedPoints,
            TotalPoints = attempt.TotalPoints,
            Passed = attempt.Passed,
            Answers = questions
                .Where(question => answersByQuestionId.ContainsKey(question.SkillModuleQuizQuestionId))
                .Select(question =>
                {
                    var answer = answersByQuestionId[question.SkillModuleQuizQuestionId];

                    var selectedOption = question.SkillModuleQuizOptions
                        .First(option => option.SkillModuleQuizOptionId == answer.SelectedOptionId);

                    return new QuizAnswerReviewDto
                    {
                        SkillModuleQuizAnswerId = answer.SkillModuleQuizAnswerId,
                        SkillModuleQuizQuestionId = question.SkillModuleQuizQuestionId,
                        QuestionText = question.QuestionText,
                        QuestionExplanation = null,
                        SelectedOptionId = selectedOption.SkillModuleQuizOptionId,
                        SelectedOptionText = selectedOption.OptionText,
                        CorrectOptionId = null,
                        CorrectOptionText = null,
                        IsCorrect = answer.IsCorrect,
                        EarnedPoints = answer.EarnedPoints,
                        QuestionPoints = question.Points
                    };
                })
                .ToList()
        };
    }
}
