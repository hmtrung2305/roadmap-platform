using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.SkillGapAnalysis.CategoryConfig;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Infrastructure.Services.SkillGapAnalysis;

namespace RoadmapPlatform.Tests;

public sealed class SkillGapConfigurationTests
{
    [Fact]
    public async Task TC158_GenerateCategoryConfiguration_CreatesConsecutiveOrdersStartingAtOne()
    {
        await using var db = TestDbContextFactory.Create();
        var scenario = await SkillGapScenarioFactory.CreateAsync(
            db,
            published: true,
            includeCategoryConfiguration: false);
        var service = new SkillGapCategoryConfigService(db);

        await service.GenerateCategoryConfigurationAsync(scenario.RoadmapId);

        var configs = await db.SkillGapCategoryConfigs
            .AsNoTracking()
            .OrderBy(config => config.DisplayOrder)
            .ToListAsync();
        Assert.Equal(3, configs.Count);
        Assert.Equal(new[] { 1, 2, 3 }, configs.Select(config => config.DisplayOrder));
        Assert.Equal(new[] { "Backend", "Frontend", "Testing" }, configs.Select(config => config.CategoryName));
    }

    [Fact]
    public async Task TC159_UpdateCategoryConfiguration_WithDuplicateNames_IsRejectedAndPreviousOrderRemains()
    {
        await using var db = TestDbContextFactory.Create();
        var scenario = await SkillGapScenarioFactory.CreateAsync(db);
        var service = new SkillGapCategoryConfigService(db);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpdateCategoryDisplayOrderAsync(scenario.OwnerUserId, scenario.RoadmapId,
            [
                new UpdateCategoryDisplayOrderDto { CategoryName = "Testing", DisplayOrder = 1 },
                new UpdateCategoryDisplayOrderDto { CategoryName = "Testing", DisplayOrder = 2 },
                new UpdateCategoryDisplayOrderDto { CategoryName = "Frontend", DisplayOrder = 3 },
            ]));

        var orders = await db.SkillGapCategoryConfigs
            .AsNoTracking()
            .OrderBy(config => config.DisplayOrder)
            .Select(config => new { config.CategoryName, config.DisplayOrder })
            .ToListAsync();
        Assert.Collection(orders,
            item => { Assert.Equal("Testing", item.CategoryName); Assert.Equal(1, item.DisplayOrder); },
            item => { Assert.Equal("Backend", item.CategoryName); Assert.Equal(2, item.DisplayOrder); },
            item => { Assert.Equal("Frontend", item.CategoryName); Assert.Equal(3, item.DisplayOrder); });
    }

    [Fact]
    public async Task TC160_UpdateCategoryConfiguration_WithNonconsecutiveOrders_IsRejected()
    {
        await using var db = TestDbContextFactory.Create();
        var scenario = await SkillGapScenarioFactory.CreateAsync(db);
        var service = new SkillGapCategoryConfigService(db);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpdateCategoryDisplayOrderAsync(scenario.OwnerUserId, scenario.RoadmapId,
            [
                new UpdateCategoryDisplayOrderDto { CategoryName = "Testing", DisplayOrder = 1 },
                new UpdateCategoryDisplayOrderDto { CategoryName = "Backend", DisplayOrder = 3 },
                new UpdateCategoryDisplayOrderDto { CategoryName = "Frontend", DisplayOrder = 4 },
            ]));

        Assert.Contains("consecutive starting from 1", exception.Message);
    }
}
