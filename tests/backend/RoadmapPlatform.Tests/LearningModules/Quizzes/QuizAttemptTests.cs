using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.LearningModules;
using RoadmapPlatform.Application.Exceptions;

namespace RoadmapPlatform.Tests.LearningModules.Quizzes;

public sealed class QuizAttemptTests
{
    [Fact]
    public async Task TC215_StartQuizAttemptAsync_WhenAnyLessonIsIncomplete_ShouldRejectQuizStart()
    {
        await using var fixture = await QuizAttemptTestFixture.CreateAsync(
            allLessonsCompleted: false);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            fixture.Service.StartQuizAttemptAsync(
                fixture.UserId,
                fixture.ModuleId,
                CancellationToken.None));

        Assert.Equal(
            "Complete all lessons before starting the quiz.",
            exception.Message);
        Assert.Empty(await fixture.Context.SkillModuleQuizAttempts.ToListAsync());
    }

    [Fact]
    public async Task TC216_StartQuizAttemptAsync_WhenAllLessonsAreCompleted_ShouldCreateInProgressAttemptWithQuestions()
    {
        await using var fixture = await QuizAttemptTestFixture.CreateAsync(
            allLessonsCompleted: true,
            maxAttempts: 3);

        var result = await fixture.Service.StartQuizAttemptAsync(
            fixture.UserId,
            fixture.ModuleId,
            CancellationToken.None);

        Assert.Equal(LearningModuleQuizAttemptStatusValues.InProgress, result.Status);
        Assert.Equal(1, result.AttemptNo);
        Assert.Equal(fixture.QuizId, result.SkillModuleQuizId);
        Assert.Equal(2, result.Quiz.Questions.Count);
        Assert.All(result.Quiz.Questions, question => Assert.Equal(2, question.Options.Count));
        Assert.DoesNotContain(
            result.Quiz.Questions.SelectMany(question => question.Options),
            option => option.IsCorrect);

        var persistedAttempt = Assert.Single(
            await fixture.Context.SkillModuleQuizAttempts.ToListAsync());
        Assert.Equal(result.SkillModuleQuizAttemptId, persistedAttempt.SkillModuleQuizAttemptId);
        Assert.Equal(LearningModuleQuizAttemptStatusValues.InProgress, persistedAttempt.Status);
    }

    [Fact]
    public async Task TC217_StartQuizAttemptAsync_WhenInProgressAttemptExists_ShouldResumeSameAttemptWithoutCreatingAnother()
    {
        await using var fixture = await QuizAttemptTestFixture.CreateAsync();
        var existingAttempt = await fixture.AddInProgressAttemptAsync();

        var result = await fixture.Service.StartQuizAttemptAsync(
            fixture.UserId,
            fixture.ModuleId,
            CancellationToken.None);

        Assert.Equal(existingAttempt.SkillModuleQuizAttemptId, result.SkillModuleQuizAttemptId);
        Assert.Equal(existingAttempt.AttemptNo, result.AttemptNo);
        Assert.Equal(new DateTimeOffset(existingAttempt.StartedAt), result.StartedAt);
        Assert.Equal(LearningModuleQuizAttemptStatusValues.InProgress, result.Status);
        Assert.Equal(2, result.Quiz.Questions.Count);

        var attempts = await fixture.Context.SkillModuleQuizAttempts.ToListAsync();
        var persistedAttempt = Assert.Single(attempts);
        Assert.Equal(existingAttempt.SkillModuleQuizAttemptId, persistedAttempt.SkillModuleQuizAttemptId);
    }

    [Fact]
    public async Task TC218_StartQuizAttemptAsync_WhenDailyAttemptLimitIsReached_ShouldRejectWithoutCreatingAttempt()
    {
        const int dailyLimit = 2;
        await using var fixture = await QuizAttemptTestFixture.CreateAsync(
            maxAttempts: dailyLimit);
        await fixture.AddSubmittedAttemptTodayAsync(attemptNo: 1);
        await fixture.AddSubmittedAttemptTodayAsync(attemptNo: 2);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            fixture.Service.StartQuizAttemptAsync(
                fixture.UserId,
                fixture.ModuleId,
                CancellationToken.None));

        Assert.Equal(
            "Maximum quiz attempts for today reached. Try again tomorrow.",
            exception.Message);

        fixture.Context.ChangeTracker.Clear();
        var attempts = await fixture.Context.SkillModuleQuizAttempts.ToListAsync();
        Assert.Equal(dailyLimit, attempts.Count);
        Assert.DoesNotContain(
            attempts,
            attempt => attempt.Status == LearningModuleQuizAttemptStatusValues.InProgress);
    }

    [Fact]
    public async Task TC219_SubmitQuizAttemptAsync_WhenOneQuestionIsUnanswered_ShouldRejectAndKeepAttemptResumable()
    {
        await using var fixture = await QuizAttemptTestFixture.CreateAsync();
        var attempt = await fixture.AddInProgressAttemptAsync();
        var answeredQuestion = fixture.Questions[0];
        var request = new SubmitQuizAttemptRequestDto
        {
            Answers =
            [
                new SubmitQuizAnswerRequestDto
                {
                    SkillModuleQuizQuestionId = answeredQuestion.QuestionId,
                    SelectedOptionId = answeredQuestion.CorrectOptionId
                }
            ]
        };

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            fixture.Service.SubmitQuizAttemptAsync(
                fixture.UserId,
                fixture.ModuleId,
                attempt.SkillModuleQuizAttemptId,
                request,
                CancellationToken.None));

        Assert.Equal(
            "Submit request must include one answer for every quiz question.",
            exception.Message);

        fixture.Context.ChangeTracker.Clear();
        var persistedAttempt = await fixture.Context.SkillModuleQuizAttempts.SingleAsync();
        Assert.Equal(LearningModuleQuizAttemptStatusValues.InProgress, persistedAttempt.Status);
        Assert.Null(persistedAttempt.SubmittedAt);
        Assert.Null(persistedAttempt.ScorePercent);
        Assert.Null(persistedAttempt.Passed);
        Assert.Empty(await fixture.Context.SkillModuleQuizAnswers.ToListAsync());
    }

    [Fact]
    public async Task TC220_SubmitQuizAttemptAsync_WhenSingleChoiceQuestionHasTwoAnswers_ShouldRejectWithoutFinalizingScore()
    {
        await using var fixture = await QuizAttemptTestFixture.CreateAsync();
        var attempt = await fixture.AddInProgressAttemptAsync();
        var firstQuestion = fixture.Questions[0];
        var secondQuestion = fixture.Questions[1];
        var request = new SubmitQuizAttemptRequestDto
        {
            Answers =
            [
                new SubmitQuizAnswerRequestDto
                {
                    SkillModuleQuizQuestionId = firstQuestion.QuestionId,
                    SelectedOptionId = firstQuestion.CorrectOptionId
                },
                new SubmitQuizAnswerRequestDto
                {
                    SkillModuleQuizQuestionId = firstQuestion.QuestionId,
                    SelectedOptionId = firstQuestion.IncorrectOptionId
                },
                new SubmitQuizAnswerRequestDto
                {
                    SkillModuleQuizQuestionId = secondQuestion.QuestionId,
                    SelectedOptionId = secondQuestion.CorrectOptionId
                }
            ]
        };

        await Assert.ThrowsAsync<ConflictException>(() =>
            fixture.Service.SubmitQuizAttemptAsync(
                fixture.UserId,
                fixture.ModuleId,
                attempt.SkillModuleQuizAttemptId,
                request,
                CancellationToken.None));

        fixture.Context.ChangeTracker.Clear();
        var persistedAttempt = await fixture.Context.SkillModuleQuizAttempts.SingleAsync();
        Assert.Equal(LearningModuleQuizAttemptStatusValues.InProgress, persistedAttempt.Status);
        Assert.Null(persistedAttempt.SubmittedAt);
        Assert.Null(persistedAttempt.ScorePercent);
        Assert.Null(persistedAttempt.EarnedPoints);
        Assert.Null(persistedAttempt.TotalPoints);
        Assert.Null(persistedAttempt.Passed);
        Assert.Empty(await fixture.Context.SkillModuleQuizAnswers.ToListAsync());
    }

    [Fact]
    public async Task TC221_SubmitQuizAttemptAsync_WhenScoreMeetsThreshold_ShouldPassAndCompleteModule()
    {
        await using var fixture = await QuizAttemptTestFixture.CreateAsync(
            passingScorePercent: 60m);
        var attempt = await fixture.AddInProgressAttemptAsync();
        var request = new SubmitQuizAttemptRequestDto
        {
            Answers = fixture.Questions
                .Select(question => new SubmitQuizAnswerRequestDto
                {
                    SkillModuleQuizQuestionId = question.QuestionId,
                    SelectedOptionId = question.CorrectOptionId
                })
                .ToList()
        };

        var result = await fixture.Service.SubmitQuizAttemptAsync(
            fixture.UserId,
            fixture.ModuleId,
            attempt.SkillModuleQuizAttemptId,
            request,
            CancellationToken.None);

        Assert.Equal(LearningModuleQuizAttemptStatusValues.Submitted, result.Status);
        Assert.Equal(100m, result.ScorePercent);
        Assert.Equal(2, result.EarnedPoints);
        Assert.Equal(2, result.TotalPoints);
        Assert.True(result.Passed == true);
        Assert.All(result.Answers, answer => Assert.True(answer.IsCorrect));

        fixture.Context.ChangeTracker.Clear();
        var persistedAttempt = await fixture.Context.SkillModuleQuizAttempts.SingleAsync();
        Assert.Equal(LearningModuleQuizAttemptStatusValues.Submitted, persistedAttempt.Status);
        Assert.Equal(100m, persistedAttempt.ScorePercent);
        Assert.True(persistedAttempt.Passed == true);
        Assert.Equal(2, await fixture.Context.SkillModuleQuizAnswers.CountAsync());

        var enrollment = await fixture.Context.SkillModuleEnrollments.SingleAsync();
        Assert.Equal(100m, enrollment.ProgressPercent);
        Assert.Equal(LearningModuleEnrollmentStatusValues.Completed, enrollment.Status);
        Assert.NotNull(enrollment.CompletedAt);
    }

    [Fact]
    public async Task TC222_SubmitQuizAttemptAsync_WhenScoreIsBelowThreshold_ShouldFailWithoutCompletingQuizAndAllowLimitedRetry()
    {
        await using var fixture = await QuizAttemptTestFixture.CreateAsync(
            maxAttempts: 2,
            passingScorePercent: 75m);
        var attempt = await fixture.AddInProgressAttemptAsync();
        var firstQuestion = fixture.Questions[0];
        var secondQuestion = fixture.Questions[1];
        var request = new SubmitQuizAttemptRequestDto
        {
            Answers =
            [
                new SubmitQuizAnswerRequestDto
                {
                    SkillModuleQuizQuestionId = firstQuestion.QuestionId,
                    SelectedOptionId = firstQuestion.CorrectOptionId
                },
                new SubmitQuizAnswerRequestDto
                {
                    SkillModuleQuizQuestionId = secondQuestion.QuestionId,
                    SelectedOptionId = secondQuestion.IncorrectOptionId
                }
            ]
        };

        var result = await fixture.Service.SubmitQuizAttemptAsync(
            fixture.UserId,
            fixture.ModuleId,
            attempt.SkillModuleQuizAttemptId,
            request,
            CancellationToken.None);

        Assert.Equal(LearningModuleQuizAttemptStatusValues.Submitted, result.Status);
        Assert.Equal(50m, result.ScorePercent);
        Assert.Equal(1, result.EarnedPoints);
        Assert.Equal(2, result.TotalPoints);
        Assert.False(result.Passed == true);

        fixture.Context.ChangeTracker.Clear();
        var enrollment = await fixture.Context.SkillModuleEnrollments.SingleAsync();
        Assert.Equal(66.67m, enrollment.ProgressPercent);
        Assert.Equal(LearningModuleEnrollmentStatusValues.InProgress, enrollment.Status);
        Assert.Null(enrollment.CompletedAt);

        var retry = await fixture.Service.StartQuizAttemptAsync(
            fixture.UserId,
            fixture.ModuleId,
            CancellationToken.None);

        Assert.Equal(2, retry.AttemptNo);
        Assert.Equal(LearningModuleQuizAttemptStatusValues.InProgress, retry.Status);
        Assert.NotEqual(attempt.SkillModuleQuizAttemptId, retry.SkillModuleQuizAttemptId);
        Assert.Equal(2, await fixture.Context.SkillModuleQuizAttempts.CountAsync());
    }
}
