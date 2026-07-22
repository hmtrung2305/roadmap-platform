using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.LearningResources;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services.LearningResources;

namespace RoadmapPlatform.Tests.LearningModules.Resources;

internal sealed class LearningResourcesTestFixture : IAsyncDisposable
{
    private LearningResourcesTestFixture(TestApplicationDbContext dbContext)
    {
        DbContext = dbContext;
        Service = new ContentLearningResourceCatalogService(dbContext);
    }

    internal TestApplicationDbContext DbContext { get; }

    internal ContentLearningResourceCatalogService Service { get; }

    internal Guid SkillId { get; private set; }

    internal static async Task<LearningResourcesTestFixture> CreateAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"learning-resources-{Guid.NewGuid():N}")
            .Options;

        var fixture = new LearningResourcesTestFixture(new TestApplicationDbContext(options));
        await fixture.SeedSkillAsync();
        return fixture;
    }

    internal static CreateContentLearningResourceRequestDto CreateValidRequest(
        string? url = null,
        string title = "ASP.NET Core Documentation")
    {
        return new CreateContentLearningResourceRequestDto
        {
            Title = title,
            Url = url ?? $"https://example.com/resources/{Guid.NewGuid():N}",
            ResourceType = "documentation",
            Description = "Official learning material for ASP.NET Core.",
            Provider = "Example Academy",
            DifficultyLevel = "intermediate"
        };
    }

    internal async Task<LearningResource> SeedResourceAsync(string url)
    {
        var now = DateTime.UtcNow;
        var resource = new LearningResource
        {
            LearningResourceId = Guid.NewGuid(),
            Title = "Existing Learning Resource",
            Url = url,
            ResourceType = "course",
            Description = "Existing resource used by the test.",
            Provider = "Existing Provider",
            DifficultyLevel = "beginner",
            LanguageCode = "en",
            VerificationStatus = "verified",
            CreatedAt = now,
            UpdatedAt = now
        };

        DbContext.LearningResources.Add(resource);
        await DbContext.SaveChangesAsync();
        return resource;
    }

    internal Task<int> CountResourcesAsync()
    {
        return DbContext.LearningResources.CountAsync();
    }

    private async Task SeedSkillAsync()
    {
        var now = DateTime.UtcNow;
        SkillId = Guid.NewGuid();

        DbContext.Skills.Add(new Skill
        {
            SkillId = SkillId,
            Name = "ASP.NET Core",
            Slug = $"aspnet-core-{Guid.NewGuid():N}",
            Description = "Backend web development skill.",
            Category = "backend",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });

        await DbContext.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await DbContext.Database.EnsureDeletedAsync();
        await DbContext.DisposeAsync();
    }

    internal sealed class TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<SkillModuleChunk>().Ignore(chunk => chunk.Embedding);
        }
    }
}
