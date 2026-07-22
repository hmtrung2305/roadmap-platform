using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.LearningModules;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services.LearningModules;
using RoadmapPlatform.Tests.TestInfrastructure;

namespace RoadmapPlatform.Tests.LearningModules.Authoring;

public sealed class ModuleAuthoringTests
{
    [Fact]
    [Trait("TestCaseId", "TC173")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Module Authoring")]
    [Trait("TestType", "Integration")]
    public async Task TC173_CreateModule_ShouldPersistEditableDraft()
    {
        var ownerId = Guid.NewGuid();
        await using var context = Tester4TestSupport.CreateContext();
        var skill = Tester4TestSupport.CreateSkill();
        context.Skills.Add(skill);
        await context.SaveChangesAsync();

        var result = await CreateService(context).CreateModuleAsync(
            ownerId,
            new CreateLearningModuleRequestDto
            {
                SkillId = skill.SkillId,
                Title = " ASP.NET Core Fundamentals ",
                Slug = "aspnet-core-fundamentals",
                Description = " Backend foundations ",
                DifficultyLevel = "beginner",
                EstimatedHours = 8
            },
            CancellationToken.None);

        Assert.Equal(LearningModuleStatusValues.Draft, result.Status);
        Assert.Equal(ownerId, result.CreatedByUserId);
        Assert.Equal("ASP.NET Core Fundamentals", result.Title);
        Assert.Equal("aspnet-core-fundamentals", result.Slug);
        Assert.True(await context.SkillModules.AnyAsync(module => module.SkillModuleId == result.SkillModuleId));
    }

    [Fact]
    [Trait("TestCaseId", "TC175")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Module Authoring")]
    [Trait("TestType", "Integration")]
    public async Task TC175_UpdateDraftMetadata_ShouldPersistChanges()
    {
        var ownerId = Guid.NewGuid();
        await using var context = Tester4TestSupport.CreateContext();
        var skill = Tester4TestSupport.CreateSkill();
        var module = Tester4TestSupport.CreateModule(ownerId, skill.SkillId, LearningModuleStatusValues.Draft);
        context.Skills.Add(skill);
        context.SkillModules.Add(module);
        await context.SaveChangesAsync();

        var result = await CreateService(context).UpdateModuleAsync(
            ownerId,
            module.SkillModuleId,
            new UpdateLearningModuleRequestDto
            {
                Title = "Advanced C#",
                Description = "Updated summary",
                DifficultyLevel = "advanced",
                EstimatedHours = 12
            },
            CancellationToken.None);

        Assert.Equal("Advanced C#", result.Title);
        Assert.Equal("Updated summary", result.Description);
        Assert.Equal("advanced", result.DifficultyLevel);
        Assert.Equal(12m, result.EstimatedHours);

        context.ChangeTracker.Clear();
        var saved = await context.SkillModules.SingleAsync(item => item.SkillModuleId == module.SkillModuleId);
        Assert.Equal("Advanced C#", saved.Title);
        Assert.Equal("Updated summary", saved.Description);
    }

    [Fact]
    [Trait("TestCaseId", "TC176")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Module Authoring")]
    [Trait("TestType", "Integration")]
    public async Task TC176_PublishedModuleMetadata_ShouldNotBeEditable()
    {
        var ownerId = Guid.NewGuid();
        await using var context = Tester4TestSupport.CreateContext();
        var skill = Tester4TestSupport.CreateSkill();
        var module = Tester4TestSupport.CreateModule(ownerId, skill.SkillId, LearningModuleStatusValues.Published);
        context.Skills.Add(skill);
        context.SkillModules.Add(module);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() => CreateService(context).UpdateModuleAsync(
            ownerId,
            module.SkillModuleId,
            new UpdateLearningModuleRequestDto { Title = "Changed published title" },
            CancellationToken.None));

        context.ChangeTracker.Clear();
        var saved = await context.SkillModules.SingleAsync(item => item.SkillModuleId == module.SkillModuleId);
        Assert.Equal("C# Basics", saved.Title);
    }

    [Fact]
    [Trait("TestCaseId", "TC177")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Module Authoring")]
    [Trait("TestType", "Integration")]
    public async Task TC177_DeleteDraftModule_ShouldRemoveIt()
    {
        var ownerId = Guid.NewGuid();
        await using var context = Tester4TestSupport.CreateContext();
        var skill = Tester4TestSupport.CreateSkill();
        var module = Tester4TestSupport.CreateModule(ownerId, skill.SkillId, LearningModuleStatusValues.Draft);
        context.Skills.Add(skill);
        context.SkillModules.Add(module);
        await context.SaveChangesAsync();

        await CreateService(context).DeleteDraftModuleAsync(
            ownerId,
            module.SkillModuleId,
            CancellationToken.None);

        Assert.False(await context.SkillModules.AnyAsync(item => item.SkillModuleId == module.SkillModuleId));
    }

    [Fact]
    [Trait("TestCaseId", "TC178")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Module Authoring")]
    [Trait("TestType", "Integration")]
    public async Task TC178_PublishedModule_ShouldNotBeDeleted()
    {
        var ownerId = Guid.NewGuid();
        await using var context = Tester4TestSupport.CreateContext();
        var skill = Tester4TestSupport.CreateSkill();
        var module = Tester4TestSupport.CreateModule(ownerId, skill.SkillId, LearningModuleStatusValues.Published);
        context.Skills.Add(skill);
        context.SkillModules.Add(module);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() => CreateService(context).DeleteDraftModuleAsync(
            ownerId,
            module.SkillModuleId,
            CancellationToken.None));

        Assert.True(await context.SkillModules.AnyAsync(item => item.SkillModuleId == module.SkillModuleId));
    }

    [Fact]
    [Trait("TestCaseId", "TC203")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Module Authoring")]
    [Trait("TestType", "Integration")]
    public async Task TC203_DraftPreview_ShouldBeAvailableToOwnerButHiddenFromLearnerCatalog()
    {
        var ownerId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();
        await using var context = Tester4TestSupport.CreateContext();
        var storage = new FakeFileStorage();
        var skill = Tester4TestSupport.CreateSkill();
        var module = Tester4TestSupport.CreateModule(ownerId, skill.SkillId, LearningModuleStatusValues.Draft);
        context.Skills.Add(skill);
        context.SkillModules.Add(module);
        await context.SaveChangesAsync();

        var preview = await CreateService(context, storage).GetPreviewAsync(
            ownerId,
            module.SkillModuleId,
            CancellationToken.None);
        var learnerService = new LearnerLearningModuleService(context, storage);
        var catalog = await learnerService.GetPublishedModulesAsync(CancellationToken.None);

        Assert.Equal(module.SkillModuleId, preview.SkillModuleId);
        Assert.DoesNotContain(catalog, item => item.SkillModuleId == module.SkillModuleId);
        await Assert.ThrowsAsync<NotFoundException>(() => learnerService.EnrollAsync(
            learnerId,
            module.SkillModuleId,
            CancellationToken.None));
    }

    private static ContentManagerLearningModuleService CreateService(
        RoadmapPlatform.Infrastructure.Data.ApplicationDbContext context,
        FakeFileStorage? storage = null)
    {
        return new ContentManagerLearningModuleService(context, storage ?? new FakeFileStorage());
    }
}
