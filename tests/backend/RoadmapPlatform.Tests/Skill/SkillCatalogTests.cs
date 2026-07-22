using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.Skills;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services.Skills;

namespace RoadmapPlatform.Tests;

public sealed class SkillCatalogTests
{
    [Fact]
    public async Task TC083_SearchSkills_ByPartialName_ReturnsJavaMatches()
    {
        await using var db = TestDbContextFactory.Create();
        db.Skills.AddRange(
            TestEntityFactory.CreateSkill("Java", "java"),
            TestEntityFactory.CreateSkill("JavaScript", "javascript"),
            TestEntityFactory.CreateSkill("Python", "python"));
        await db.SaveChangesAsync();
        var service = new ContentSkillCatalogService(db);

        var result = await service.SearchSkillsAsync(
            new ContentSkillSearchQueryDto { Search = "java" },
            CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, skill =>
            Assert.True(skill.Name.Contains("java", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task TC084_SearchSkills_WithCategoryFilter_ReturnsOnlySelectedCategory()
    {
        await using var db = TestDbContextFactory.Create();
        db.Skills.AddRange(
            TestEntityFactory.CreateSkill("React", "react", "Frontend"),
            TestEntityFactory.CreateSkill("ASP.NET Core", "asp-net-core", "Backend"),
            TestEntityFactory.CreateSkill("Node.js", "node-js", "Backend"));
        await db.SaveChangesAsync();
        var service = new ContentSkillCatalogService(db);

        var result = await service.SearchSkillsAsync(
            new ContentSkillSearchQueryDto { Search = "n", Category = "Backend" },
            CancellationToken.None);

        Assert.NotEmpty(result.Items);
        Assert.All(result.Items, skill => Assert.Equal("Backend", skill.Category));
    }

    [Fact]
    public async Task TC085_SearchSkills_WithUnmatchedQuery_ReturnsEmptyResult()
    {
        await using var db = TestDbContextFactory.Create();
        db.Skills.Add(TestEntityFactory.CreateSkill("Java", "java"));
        await db.SaveChangesAsync();
        var service = new ContentSkillCatalogService(db);

        var result = await service.SearchSkillsAsync(
            new ContentSkillSearchQueryDto { Search = "unique-nonexistent-term" },
            CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task TC086_CreateSkill_WithUniqueName_CreatesSearchableSlug()
    {
        await using var db = TestDbContextFactory.Create();
        var service = new ContentSkillCatalogService(db);

        var created = await service.CreateSkillAsync(new CreateContentSkillRequestDto
        {
            Name = "Contract Testing",
            Category = "Testing",
            Description = "API contract verification",
        }, CancellationToken.None);

        Assert.Equal("contract-testing", created.Slug);
        var search = await service.SearchSkillsAsync(
            new ContentSkillSearchQueryDto { Search = "contract" },
            CancellationToken.None);
        Assert.Contains(search.Items, skill => skill.SkillId == created.SkillId);
    }

    [Fact]
    public async Task TC087_CreateSkill_WithDuplicateName_ThrowsConflict()
    {
        await using var db = TestDbContextFactory.Create();
        db.Skills.Add(TestEntityFactory.CreateSkill("Java", "java"));
        await db.SaveChangesAsync();
        var service = new ContentSkillCatalogService(db);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateSkillAsync(new CreateContentSkillRequestDto
            {
                Name = " java ",
                Category = "Programming",
            }, CancellationToken.None));

        Assert.Equal(1, await db.Skills.CountAsync());
    }

    [Fact]
    public async Task TC088_CreateSkill_WithDuplicateGeneratedSlug_ThrowsConflict()
    {
        await using var db = TestDbContextFactory.Create();
        db.Skills.Add(TestEntityFactory.CreateSkill("C Sharp", "c-sharp"));
        await db.SaveChangesAsync();
        var service = new ContentSkillCatalogService(db);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateSkillAsync(new CreateContentSkillRequestDto
            {
                Name = "C# Sharp",
                Category = "Programming",
            }, CancellationToken.None));
    }

    [Fact]
    public async Task TC089_UpdateUnusedSkill_WithValidValues_UpdatesSearchResult()
    {
        await using var db = TestDbContextFactory.Create();
        var skill = TestEntityFactory.CreateSkill("Old Skill", "old-skill");
        db.Skills.Add(skill);
        await db.SaveChangesAsync();
        var service = new ContentSkillCatalogService(db);

        var updated = await service.UpdateSkillAsync(skill.SkillId, new UpdateContentSkillRequestDto
        {
            Name = "Modern Testing",
            Category = "Testing",
            Description = "Updated description",
        }, CancellationToken.None);

        Assert.Equal("Modern Testing", updated.Name);
        Assert.Equal("modern-testing", updated.Slug);
        var search = await service.SearchSkillsAsync(
            new ContentSkillSearchQueryDto { Search = "modern" },
            CancellationToken.None);
        Assert.Single(search.Items);
    }

    [Fact]
    public async Task TC090_UpdateSkill_WhenAlreadyUsed_ThrowsConflictAndKeepsOriginalValues()
    {
        await using var db = TestDbContextFactory.Create();
        var skill = TestEntityFactory.CreateSkill("Used Skill", "used-skill");
        var module = new SkillModule
        {
            SkillModuleId = Guid.NewGuid(),
            SkillId = skill.SkillId,
            Skill = skill,
            Title = "Used skill module",
            Slug = "used-skill-module",
            Status = "draft",
            Metadata = "{}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        skill.SkillModules.Add(module);
        db.Add(skill);
        await db.SaveChangesAsync();
        var service = new ContentSkillCatalogService(db);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpdateSkillAsync(skill.SkillId, new UpdateContentSkillRequestDto
            {
                Name = "Changed Skill",
                Category = "Testing",
            }, CancellationToken.None));

        var stored = await db.Skills.SingleAsync();
        Assert.Equal("Used Skill", stored.Name);
    }
}
