using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.SkillGapAnalysis.Analysis;
using RoadmapPlatform.Application.DTOs.SkillGapAnalysis.History;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Infrastructure.Services.SkillGapAnalysis;

namespace RoadmapPlatform.Tests;

public sealed class SkillGapTests
{
    [Fact]
    public async Task TC151_Analyze_WithSelectedKnownSkills_GroupsResultsAndSavesHistory()
    {
        await using var db = TestDbContextFactory.Create();
        var scenario = await SkillGapScenarioFactory.CreateAsync(db);
        var service = new SkillGapAnalysisService(db);

        var result = await service.AnalyzeAsync(scenario.LearnerUserId, new AnalyzeSkillGapRequestDto
        {
            RoadmapId = scenario.RoadmapId,
            SelectedSkillIds = [scenario.TestingSkillId],
        }, CancellationToken.None);

        Assert.Equal(1, result.MatchedSkills);
        Assert.Equal(3, result.TotalSkills);
        Assert.Equal(2, result.MissingSkills);
        Assert.Equal(new[] { "Testing", "Backend", "Frontend" }, result.Categories.Select(item => item.CategoryName));
        Assert.True(result.Categories[0].Skills.Single().IsMatched);
        Assert.Equal(1, await db.SkillGapAnalysisHistories.CountAsync());
    }

    [Fact]
    public async Task TC152_Analyze_WithSkillOutsideRoadmap_RejectsAndDoesNotSaveHistory()
    {
        await using var db = TestDbContextFactory.Create();
        var scenario = await SkillGapScenarioFactory.CreateAsync(db);
        var service = new SkillGapAnalysisService(db);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.AnalyzeAsync(scenario.LearnerUserId, new AnalyzeSkillGapRequestDto
            {
                RoadmapId = scenario.RoadmapId,
                SelectedSkillIds = [Guid.NewGuid()],
            }, CancellationToken.None));

        Assert.Empty(db.SkillGapAnalysisHistories);
    }

    [Fact]
    public async Task TC153_Analyze_WithNoKnownSkills_ReportsAllSkillsMissingAndSavesHistory()
    {
        await using var db = TestDbContextFactory.Create();
        var scenario = await SkillGapScenarioFactory.CreateAsync(db);
        var service = new SkillGapAnalysisService(db);

        var result = await service.AnalyzeAsync(scenario.LearnerUserId, new AnalyzeSkillGapRequestDto
        {
            RoadmapId = scenario.RoadmapId,
            SelectedSkillIds = [],
        }, CancellationToken.None);

        Assert.Equal(0, result.MatchedSkills);
        Assert.Equal(3, result.MissingSkills);
        Assert.All(result.Categories.SelectMany(category => category.Skills), skill => Assert.False(skill.IsMatched));
        Assert.Equal(1, await db.SkillGapAnalysisHistories.CountAsync());
    }

    [Fact]
    public async Task TC154_Analyze_RoadmapWithoutPublishedVersion_IsRejected()
    {
        await using var db = TestDbContextFactory.Create();
        var scenario = await SkillGapScenarioFactory.CreateAsync(
            db,
            published: false,
            includeCategoryConfiguration: false);
        var service = new SkillGapAnalysisService(db);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.AnalyzeAsync(scenario.LearnerUserId, new AnalyzeSkillGapRequestDto
            {
                RoadmapId = scenario.RoadmapId,
                SelectedSkillIds = [],
            }, CancellationToken.None));

        Assert.Empty(db.SkillGapAnalysisHistories);
    }

    [Fact]
    public async Task TC155_ListAndOpenHistory_ReturnsOnlyOwnRecordsAndSnapshotDetail()
    {
        await using var db = TestDbContextFactory.Create();
        var scenario = await SkillGapScenarioFactory.CreateAsync(db);
        var first = SkillGapScenarioFactory.CreateHistory(
            scenario,
            scenario.LearnerUserId,
            DateTime.UtcNow.AddMinutes(-2));
        var second = SkillGapScenarioFactory.CreateHistory(
            scenario,
            scenario.LearnerUserId,
            DateTime.UtcNow.AddMinutes(-1));
        var foreign = SkillGapScenarioFactory.CreateHistory(
            scenario,
            scenario.OtherLearnerUserId,
            DateTime.UtcNow);
        db.SkillGapAnalysisHistories.AddRange(first, second, foreign);
        await db.SaveChangesAsync();
        var service = new SkillGapHistoryService(db);

        var page = await service.GetHistoryAsync(
            scenario.LearnerUserId,
            new SkillGapHistoryPageRequestDto { Limit = 20 },
            CancellationToken.None);
        var detail = await service.GetHistoryDetailAsync(
            scenario.LearnerUserId,
            second.SkillGapAnalysisHistoryId,
            CancellationToken.None);

        Assert.Equal(2, page.Items.Count);
        Assert.All(page.Items, item => Assert.NotEqual(foreign.SkillGapAnalysisHistoryId, item.SkillGapAnalysisHistoryId));
        Assert.Equal(second.SkillGapAnalysisHistoryId, detail.SkillGapAnalysisHistoryId);
        Assert.Equal(scenario.RoadmapSlug, detail.RoadmapSlug);
    }

    [Fact]
    public async Task TC156_DeleteOwnHistory_RemovesItemAndDetailBecomesUnavailable()
    {
        await using var db = TestDbContextFactory.Create();
        var scenario = await SkillGapScenarioFactory.CreateAsync(db);
        var history = SkillGapScenarioFactory.CreateHistory(
            scenario,
            scenario.LearnerUserId,
            DateTime.UtcNow);
        db.SkillGapAnalysisHistories.Add(history);
        await db.SaveChangesAsync();
        var service = new SkillGapHistoryService(db);

        await service.DeleteHistoryAsync(
            scenario.LearnerUserId,
            history.SkillGapAnalysisHistoryId,
            CancellationToken.None);

        Assert.Empty(db.SkillGapAnalysisHistories);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetHistoryDetailAsync(
                scenario.LearnerUserId,
                history.SkillGapAnalysisHistoryId,
                CancellationToken.None));
    }

    [Fact]
    public async Task TC157_OpenAnotherLearnersHistory_IsRejectedWithoutDisclosure()
    {
        await using var db = TestDbContextFactory.Create();
        var scenario = await SkillGapScenarioFactory.CreateAsync(db);
        var foreign = SkillGapScenarioFactory.CreateHistory(
            scenario,
            scenario.OtherLearnerUserId,
            DateTime.UtcNow);
        db.SkillGapAnalysisHistories.Add(foreign);
        await db.SaveChangesAsync();
        var service = new SkillGapHistoryService(db);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetHistoryDetailAsync(
                scenario.LearnerUserId,
                foreign.SkillGapAnalysisHistoryId,
                CancellationToken.None));
    }
}
