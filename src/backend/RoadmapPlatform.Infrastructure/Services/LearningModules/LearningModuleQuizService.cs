using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.LearningModules;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.LearningModules;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.LearningModules;

public sealed class LearningModuleQuizService : ILearningModuleQuizService
{
    private readonly ApplicationDbContext _context;

    public LearningModuleQuizService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LearningModuleQuizDto> UpsertQuizAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        UpsertQuizRequestDto request,
        CancellationToken cancellationToken)
    {
        var module = await GetOwnedDraftModuleAsync(
            counselorUserId,
            skillModuleId,
            cancellationToken);

        var quiz = await _context.SkillModuleQuizzes
            .Include(item => item.SkillModuleQuizQuestions)
                .ThenInclude(question => question.SkillModuleQuizOptions)
            .FirstOrDefaultAsync(item => item.SkillModuleId == skillModuleId, cancellationToken);

        var now = DateTime.UtcNow;

        if (quiz == null)
        {
            quiz = new SkillModuleQuiz
            {
                SkillModuleQuizId = Guid.NewGuid(),
                SkillModuleId = skillModuleId,
                Title = request.Title.Trim(),
                Description = NormalizeOptionalText(request.Description),
                PassingScorePercent = NormalizePassingScore(request.PassingScorePercent),
                MaxAttempts = request.MaxAttempts,
                Status = LearningModuleStatusValues.Draft,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.SkillModuleQuizzes.Add(quiz);
        }
        else
        {
            quiz.Title = request.Title.Trim();
            quiz.Description = NormalizeOptionalText(request.Description);
            quiz.PassingScorePercent = NormalizePassingScore(request.PassingScorePercent);
            quiz.MaxAttempts = request.MaxAttempts;
            quiz.UpdatedAt = now;
        }

        module.UpdatedAt = now;

        await _context.SaveChangesAsync(cancellationToken);

        return MapQuiz(quiz);
    }

    public async Task<LearningModuleQuizQuestionDto> AddQuestionAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        UpsertQuizQuestionRequestDto request,
        CancellationToken cancellationToken)
    {
        var module = await GetOwnedDraftModuleAsync(
            counselorUserId,
            skillModuleId,
            cancellationToken);

        var quiz = await _context.SkillModuleQuizzes
            .Include(item => item.SkillModuleQuizQuestions)
            .FirstOrDefaultAsync(item => item.SkillModuleId == skillModuleId, cancellationToken);

        if (quiz == null)
        {
            throw new NotFoundException("Quiz was not found. Create the quiz before adding questions.");
        }

        ValidateQuestionRequest(request);

        var now = DateTime.UtcNow;
        var orderIndex = request.OrderIndex > 0
            ? request.OrderIndex
            : await GetNextQuestionOrderIndexAsync(quiz.SkillModuleQuizId, cancellationToken);

        var question = new SkillModuleQuizQuestion
        {
            SkillModuleQuizQuestionId = Guid.NewGuid(),
            SkillModuleQuizId = quiz.SkillModuleQuizId,
            QuestionText = request.QuestionText.Trim(),
            QuestionType = NormalizeQuestionType(request.QuestionType),
            Explanation = NormalizeOptionalText(request.Explanation),
            OrderIndex = orderIndex,
            Points = request.Points <= 0 ? 1 : request.Points,
            CreatedAt = now,
            UpdatedAt = now
        };

        question.SkillModuleQuizOptions = request.Options
            .OrderBy(option => option.OrderIndex <= 0 ? int.MaxValue : option.OrderIndex)
            .Select((option, index) => new SkillModuleQuizOption
            {
                SkillModuleQuizOptionId = Guid.NewGuid(),
                SkillModuleQuizQuestionId = question.SkillModuleQuizQuestionId,
                OptionText = option.OptionText.Trim(),
                IsCorrect = option.IsCorrect,
                Explanation = NormalizeOptionalText(option.Explanation),
                OrderIndex = option.OrderIndex > 0 ? option.OrderIndex : index + 1,
                CreatedAt = now,
                UpdatedAt = now
            })
            .ToList();

        NormalizeOptionOrder(question.SkillModuleQuizOptions);

        _context.SkillModuleQuizQuestions.Add(question);

        quiz.UpdatedAt = now;
        module.UpdatedAt = now;

        await _context.SaveChangesAsync(cancellationToken);

        return MapQuestion(question);
    }

    public async Task<LearningModuleQuizQuestionDto> UpdateQuestionAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        Guid questionId,
        UpsertQuizQuestionRequestDto request,
        CancellationToken cancellationToken)
    {
        var module = await GetOwnedDraftModuleAsync(
            counselorUserId,
            skillModuleId,
            cancellationToken);

        var quiz = await _context.SkillModuleQuizzes
            .Include(item => item.SkillModuleQuizQuestions)
                .ThenInclude(question => question.SkillModuleQuizOptions)
            .FirstOrDefaultAsync(item => item.SkillModuleId == skillModuleId, cancellationToken);

        if (quiz == null)
        {
            throw new NotFoundException("Quiz was not found.");
        }

        var question = quiz.SkillModuleQuizQuestions
            .FirstOrDefault(item => item.SkillModuleQuizQuestionId == questionId);

        if (question == null)
        {
            throw new NotFoundException("Quiz question was not found.");
        }

        ValidateQuestionRequest(request);

        var now = DateTime.UtcNow;

        question.QuestionText = request.QuestionText.Trim();
        question.QuestionType = NormalizeQuestionType(request.QuestionType);
        question.Explanation = NormalizeOptionalText(request.Explanation);
        question.OrderIndex = request.OrderIndex > 0 ? request.OrderIndex : question.OrderIndex;
        question.Points = request.Points <= 0 ? 1 : request.Points;
        question.UpdatedAt = now;

        _context.SkillModuleQuizOptions.RemoveRange(question.SkillModuleQuizOptions);

        question.SkillModuleQuizOptions = request.Options
            .OrderBy(option => option.OrderIndex <= 0 ? int.MaxValue : option.OrderIndex)
            .Select((option, index) => new SkillModuleQuizOption
            {
                SkillModuleQuizOptionId = Guid.NewGuid(),
                SkillModuleQuizQuestionId = question.SkillModuleQuizQuestionId,
                OptionText = option.OptionText.Trim(),
                IsCorrect = option.IsCorrect,
                Explanation = NormalizeOptionalText(option.Explanation),
                OrderIndex = option.OrderIndex > 0 ? option.OrderIndex : index + 1,
                CreatedAt = now,
                UpdatedAt = now
            })
            .ToList();

        NormalizeOptionOrder(question.SkillModuleQuizOptions);

        quiz.UpdatedAt = now;
        module.UpdatedAt = now;

        await _context.SaveChangesAsync(cancellationToken);

        return MapQuestion(question);
    }

    public async Task<IReadOnlyList<LearningModuleQuizQuestionDto>> ReorderQuestionsAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        ReorderQuizQuestionsRequestDto request,
        CancellationToken cancellationToken)
    {
        var module = await GetOwnedDraftModuleAsync(
            counselorUserId,
            skillModuleId,
            cancellationToken);

        var quiz = await _context.SkillModuleQuizzes
            .Include(item => item.SkillModuleQuizQuestions)
                .ThenInclude(question => question.SkillModuleQuizOptions)
            .FirstOrDefaultAsync(item => item.SkillModuleId == skillModuleId, cancellationToken);

        if (quiz == null)
        {
            throw new NotFoundException("Quiz was not found.");
        }

        ValidateReorderRequest(request, quiz.SkillModuleQuizQuestions);

        var now = DateTime.UtcNow;
        var orderMap = request.Questions.ToDictionary(
            item => item.SkillModuleQuizQuestionId,
            item => item.OrderIndex);

        foreach (var question in quiz.SkillModuleQuizQuestions)
        {
            question.OrderIndex = orderMap[question.SkillModuleQuizQuestionId];
            question.UpdatedAt = now;
        }

        quiz.UpdatedAt = now;
        module.UpdatedAt = now;

        await _context.SaveChangesAsync(cancellationToken);

        return quiz.SkillModuleQuizQuestions
            .OrderBy(question => question.OrderIndex)
            .Select(MapQuestion)
            .ToList();
    }

    public async Task DeleteQuestionAsync(
        Guid counselorUserId,
        Guid skillModuleId,
        Guid questionId,
        CancellationToken cancellationToken)
    {
        var module = await GetOwnedDraftModuleAsync(
            counselorUserId,
            skillModuleId,
            cancellationToken);

        var quiz = await _context.SkillModuleQuizzes
            .Include(item => item.SkillModuleQuizQuestions)
            .FirstOrDefaultAsync(item => item.SkillModuleId == skillModuleId, cancellationToken);

        if (quiz == null)
        {
            throw new NotFoundException("Quiz was not found.");
        }

        var question = quiz.SkillModuleQuizQuestions
            .FirstOrDefault(item => item.SkillModuleQuizQuestionId == questionId);

        if (question == null)
        {
            throw new NotFoundException("Quiz question was not found.");
        }

        _context.SkillModuleQuizQuestions.Remove(question);

        var now = DateTime.UtcNow;
        quiz.UpdatedAt = now;
        module.UpdatedAt = now;

        await _context.SaveChangesAsync(cancellationToken);
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

    private async Task<int> GetNextQuestionOrderIndexAsync(
        Guid skillModuleQuizId,
        CancellationToken cancellationToken)
    {
        var maxOrder = await _context.SkillModuleQuizQuestions
            .Where(item => item.SkillModuleQuizId == skillModuleQuizId)
            .Select(item => (int?)item.OrderIndex)
            .MaxAsync(cancellationToken);

        return (maxOrder ?? 0) + 1;
    }

    private static void ValidateQuestionRequest(UpsertQuizQuestionRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.QuestionText))
        {
            throw new ConflictException("Question text is required.");
        }

        var questionType = NormalizeQuestionType(request.QuestionType);

        if (request.Options.Count < 2)
        {
            throw new ConflictException("A question must have at least two options.");
        }

        if (request.Options.Any(option => string.IsNullOrWhiteSpace(option.OptionText)))
        {
            throw new ConflictException("Every option must have text.");
        }

        var positiveOrders = request.Options
            .Where(option => option.OrderIndex > 0)
            .Select(option => option.OrderIndex)
            .ToList();

        if (positiveOrders.Count != positiveOrders.Distinct().Count())
        {
            throw new ConflictException("Option order values must be unique.");
        }

        if (questionType == LearningModuleQuestionTypeValues.SingleChoice
            && request.Options.Count(option => option.IsCorrect) != 1)
        {
            throw new ConflictException("A single-choice question must have exactly one correct option.");
        }
    }

    private static void ValidateReorderRequest(
        ReorderQuizQuestionsRequestDto request,
        ICollection<SkillModuleQuizQuestion> existingQuestions)
    {
        if (request.Questions.Count == 0)
        {
            throw new ConflictException("Question order list cannot be empty.");
        }

        var existingIds = existingQuestions
            .Select(question => question.SkillModuleQuizQuestionId)
            .OrderBy(id => id)
            .ToList();

        var requestIds = request.Questions
            .Select(question => question.SkillModuleQuizQuestionId)
            .OrderBy(id => id)
            .ToList();

        if (!existingIds.SequenceEqual(requestIds))
        {
            throw new ConflictException("Question reorder request must include every question in the quiz.");
        }

        if (request.Questions.Any(question => question.OrderIndex <= 0))
        {
            throw new ConflictException("Question order values must be positive.");
        }

        if (request.Questions.Select(question => question.OrderIndex).Distinct().Count() != request.Questions.Count)
        {
            throw new ConflictException("Question order values must be unique.");
        }
    }

    private static string NormalizeQuestionType(string? questionType)
    {
        if (string.IsNullOrWhiteSpace(questionType))
        {
            return LearningModuleQuestionTypeValues.SingleChoice;
        }

        var normalized = questionType.Trim().ToLowerInvariant();

        return normalized == LearningModuleQuestionTypeValues.SingleChoice
            ? normalized
            : throw new ConflictException($"Unsupported question type: {questionType}");
    }

    private static decimal NormalizePassingScore(decimal passingScorePercent)
    {
        if (passingScorePercent <= 0 || passingScorePercent > 100)
        {
            throw new ConflictException("Passing score percent must be between 1 and 100.");
        }

        return passingScorePercent;
    }

    private static void NormalizeOptionOrder(ICollection<SkillModuleQuizOption> options)
    {
        var ordered = options
            .OrderBy(option => option.OrderIndex)
            .ThenBy(option => option.OptionText)
            .ToList();

        for (var index = 0; index < ordered.Count; index++)
        {
            ordered[index].OrderIndex = index + 1;
        }
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
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
}
