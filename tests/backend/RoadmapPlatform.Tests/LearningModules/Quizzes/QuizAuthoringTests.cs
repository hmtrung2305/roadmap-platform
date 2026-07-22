using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.LearningModules;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services.LearningModules;
using RoadmapPlatform.Tests.TestInfrastructure;

namespace RoadmapPlatform.Tests.LearningModules.Quizzes;

public sealed class QuizAuthoringTests
{
    [Fact]
    [Trait("TestCaseId", "TC188")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Quiz Authoring")]
    [Trait("TestType", "Integration")]
    public async Task TC188_ValidSingleChoiceQuestion_ShouldBeCreatedWithOneCorrectOption()
    {
        var ownerId = Guid.NewGuid();
        await using var context = Tester4TestSupport.CreateContext();
        var module = await SeedDraftModuleAndQuizAsync(context, ownerId);
        var service = new LearningModuleQuizService(context);

        var result = await service.AddQuestionAsync(
            ownerId,
            module.SkillModuleId,
            ValidQuestion("What is dependency injection?"),
            CancellationToken.None);

        Assert.Equal(LearningModuleQuestionTypeValues.SingleChoice, result.QuestionType);
        Assert.Equal(2, result.Options.Count);
        Assert.Single(result.Options, option => option.IsCorrect);
        Assert.True(await context.SkillModuleQuizQuestions.AnyAsync(
            question => question.SkillModuleQuizQuestionId == result.SkillModuleQuizQuestionId));
    }

    [Fact]
    [Trait("TestCaseId", "TC189")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Quiz Authoring")]
    [Trait("TestType", "Unit")]
    public void TC189_QuestionWithFewerThanTwoOptions_ShouldBeRejected()
    {
        var request = ValidQuestion("Invalid question");
        request.Options = [request.Options[0]];

        var exception = Assert.Throws<ConflictException>(() =>
            Tester4TestSupport.InvokePrivateStatic(
                typeof(LearningModuleQuizService),
                "ValidateQuestionRequest",
                request));

        Assert.Contains("at least two options", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("TestCaseId", "TC190")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Quiz Authoring")]
    [Trait("TestType", "Unit")]
    public void TC190_QuestionWithNoCorrectOption_ShouldBeRejected()
    {
        var request = ValidQuestion("Invalid question");
        foreach (var option in request.Options)
        {
            option.IsCorrect = false;
        }

        var exception = Assert.Throws<ConflictException>(() =>
            Tester4TestSupport.InvokePrivateStatic(
                typeof(LearningModuleQuizService),
                "ValidateQuestionRequest",
                request));

        Assert.Contains("exactly one correct", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("TestCaseId", "TC191")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Quiz Authoring")]
    [Trait("TestType", "Unit")]
    public void TC191_SingleChoiceQuestionWithMultipleCorrectOptions_ShouldBeRejected()
    {
        var request = ValidQuestion("Invalid question");
        foreach (var option in request.Options)
        {
            option.IsCorrect = true;
        }

        var exception = Assert.Throws<ConflictException>(() =>
            Tester4TestSupport.InvokePrivateStatic(
                typeof(LearningModuleQuizService),
                "ValidateQuestionRequest",
                request));

        Assert.Contains("exactly one correct", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("TestCaseId", "TC192")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Quiz Authoring")]
    [Trait("TestType", "SourceContract")]
    public void TC192_UpdateQuestion_ShouldReplaceValidFieldsAndOptionsThenReloadSavedQuestion()
    {
        var source = Tester4TestSupport.ReadRepositoryFile(
            "src", "backend", "RoadmapPlatform.Infrastructure", "Services", "LearningModules", "LearningModuleQuizService.cs");
        var methodStart = source.IndexOf("public async Task<LearningModuleQuizQuestionDto> UpdateQuestionAsync", StringComparison.Ordinal);
        var validateIndex = source.IndexOf("ValidateQuestionRequest(request);", methodStart, StringComparison.Ordinal);
        var textIndex = source.IndexOf("question.QuestionText = request.QuestionText.Trim();", methodStart, StringComparison.Ordinal);
        var deleteOptionsIndex = source.IndexOf("ExecuteDeleteAsync", methodStart, StringComparison.Ordinal);
        var addOptionsIndex = source.IndexOf("_context.SkillModuleQuizOptions.AddRange(newOptions);", methodStart, StringComparison.Ordinal);
        var reloadIndex = source.IndexOf("var savedQuestion = await _context.SkillModuleQuizQuestions", methodStart, StringComparison.Ordinal);

        Assert.True(methodStart >= 0);
        Assert.True(validateIndex > methodStart);
        Assert.True(textIndex > validateIndex);
        Assert.True(deleteOptionsIndex > textIndex);
        Assert.True(addOptionsIndex > deleteOptionsIndex);
        Assert.True(reloadIndex > addOptionsIndex);
    }

    [Fact]
    [Trait("TestCaseId", "TC193")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Quiz Authoring")]
    [Trait("TestType", "Integration")]
    public async Task TC193_ReorderQuizQuestions_ShouldPersistCompleteConsecutiveOrder()
    {
        var ownerId = Guid.NewGuid();
        await using var context = Tester4TestSupport.CreateContext();
        var module = await SeedDraftModuleAndQuizAsync(context, ownerId, questionCount: 3);
        var questions = await context.SkillModuleQuizQuestions.OrderBy(item => item.OrderIndex).ToListAsync();
        var first = questions[0];
        var second = questions[1];
        var third = questions[2];

        var result = await new LearningModuleQuizService(context).ReorderQuestionsAsync(
            ownerId,
            module.SkillModuleId,
            new ReorderQuizQuestionsRequestDto
            {
                Questions =
                [
                    new() { SkillModuleQuizQuestionId = second.SkillModuleQuizQuestionId, OrderIndex = 1 },
                    new() { SkillModuleQuizQuestionId = third.SkillModuleQuizQuestionId, OrderIndex = 2 },
                    new() { SkillModuleQuizQuestionId = first.SkillModuleQuizQuestionId, OrderIndex = 3 }
                ]
            },
            CancellationToken.None);

        Assert.Equal(
            new[] { second.SkillModuleQuizQuestionId, third.SkillModuleQuizQuestionId, first.SkillModuleQuizQuestionId },
            result.Select(item => item.SkillModuleQuizQuestionId).ToArray());
        Assert.Equal(new[] { 1, 2, 3 }, result.Select(item => item.OrderIndex).ToArray());
    }

    [Fact]
    [Trait("TestCaseId", "TC194")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Quiz Authoring")]
    [Trait("TestType", "Integration")]
    public async Task TC194_DeleteQuizQuestion_ShouldRemoveOnlySelectedQuestion()
    {
        var ownerId = Guid.NewGuid();
        await using var context = Tester4TestSupport.CreateContext();
        var module = await SeedDraftModuleAndQuizAsync(context, ownerId, questionCount: 2);
        var questions = await context.SkillModuleQuizQuestions.OrderBy(item => item.OrderIndex).ToListAsync();

        await new LearningModuleQuizService(context).DeleteQuestionAsync(
            ownerId,
            module.SkillModuleId,
            questions[0].SkillModuleQuizQuestionId,
            CancellationToken.None);

        var remaining = await context.SkillModuleQuizQuestions.ToListAsync();
        Assert.Single(remaining);
        Assert.Equal(questions[1].SkillModuleQuizQuestionId, remaining[0].SkillModuleQuizQuestionId);
    }

    private static async Task<SkillModule> SeedDraftModuleAndQuizAsync(
        RoadmapPlatform.Infrastructure.Data.ApplicationDbContext context,
        Guid ownerId,
        int questionCount = 0)
    {
        var skill = Tester4TestSupport.CreateSkill();
        var module = Tester4TestSupport.CreateModule(ownerId, skill.SkillId, LearningModuleStatusValues.Draft);
        var quiz = Tester4TestSupport.CreateQuiz(module.SkillModuleId, questionCount);
        context.Skills.Add(skill);
        context.SkillModules.Add(module);
        context.SkillModuleQuizzes.Add(quiz);
        await context.SaveChangesAsync();
        return module;
    }

    private static UpsertQuizQuestionRequestDto ValidQuestion(string text)
    {
        return new UpsertQuizQuestionRequestDto
        {
            QuestionText = text,
            QuestionType = LearningModuleQuestionTypeValues.SingleChoice,
            Explanation = "Explanation",
            Points = 1,
            Options =
            [
                new() { OptionText = "Correct", IsCorrect = true, OrderIndex = 1 },
                new() { OptionText = "Incorrect", IsCorrect = false, OrderIndex = 2 }
            ]
        };
    }
}
