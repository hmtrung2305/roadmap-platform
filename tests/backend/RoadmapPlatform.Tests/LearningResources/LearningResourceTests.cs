using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.LearningResources;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Infrastructure.Services.LearningResources;

namespace RoadmapPlatform.Tests;

public sealed class LearningResourceTests
{
    [Fact]
    public async Task TC091_CreateLearningResource_WithValidHttpsUrl_CreatesResource()
    {
        await using var db = TestDbContextFactory.Create();
        var service = new ContentLearningResourceCatalogService(db);

        var created = await service.CreateLearningResourceAsync(new CreateContentLearningResourceRequestDto
        {
            Title = "xUnit Fundamentals",
            Url = "https://example.com/xunit",
            ResourceType = "course",
            DifficultyLevel = "beginner",
            Provider = "Example Academy",
        }, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, created.ResourceId);
        Assert.Equal("verified", created.VerificationStatus);
        Assert.Equal(1, await db.LearningResources.CountAsync());
    }

    [Fact]
    public async Task TC092_CreateLearningResource_WithDuplicateUrl_ThrowsConflict()
    {
        await using var db = TestDbContextFactory.Create();
        db.LearningResources.Add(TestEntityFactory.CreateLearningResource(
            "Existing course",
            "https://example.com/course"));
        await db.SaveChangesAsync();
        var service = new ContentLearningResourceCatalogService(db);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateLearningResourceAsync(new CreateContentLearningResourceRequestDto
            {
                Title = "Duplicate course",
                Url = " HTTPS://EXAMPLE.COM/COURSE ",
                ResourceType = "course",
                DifficultyLevel = "beginner",
            }, CancellationToken.None));

        Assert.Equal(1, await db.LearningResources.CountAsync());
    }

    [Fact]
    public async Task TC093_CreateLearningResource_WithFtpUrl_ThrowsValidationError()
    {
        await using var db = TestDbContextFactory.Create();
        var service = new ContentLearningResourceCatalogService(db);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateLearningResourceAsync(new CreateContentLearningResourceRequestDto
            {
                Title = "FTP course",
                Url = "ftp://example.com/course",
                ResourceType = "course",
            }, CancellationToken.None));

        Assert.Contains("HTTP or HTTPS", exception.Message);
        Assert.Empty(db.LearningResources);
    }

    [Fact]
    public async Task TC094_CreateLearningResource_WithInvalidType_ThrowsValidationError()
    {
        await using var db = TestDbContextFactory.Create();
        var service = new ContentLearningResourceCatalogService(db);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateLearningResourceAsync(new CreateContentLearningResourceRequestDto
            {
                Title = "Unsupported resource",
                Url = "https://example.com/resource",
                ResourceType = "podcast",
            }, CancellationToken.None));

        Assert.Contains("Resource type is invalid", exception.Message);
    }

    [Fact]
    public async Task TC095_CreateLearningResource_WithInvalidDifficulty_ThrowsValidationError()
    {
        await using var db = TestDbContextFactory.Create();
        var service = new ContentLearningResourceCatalogService(db);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateLearningResourceAsync(new CreateContentLearningResourceRequestDto
            {
                Title = "Impossible course",
                Url = "https://example.com/impossible",
                ResourceType = "course",
                DifficultyLevel = "expert-only",
            }, CancellationToken.None));

        Assert.Contains("Difficulty level is invalid", exception.Message);
    }

    [Fact]
    public async Task TC096_UpdateLearningResource_WithValidMetadata_PersistsChanges()
    {
        await using var db = TestDbContextFactory.Create();
        var resource = TestEntityFactory.CreateLearningResource(
            "Old title",
            "https://example.com/course",
            "beginner");
        db.LearningResources.Add(resource);
        await db.SaveChangesAsync();
        var service = new ContentLearningResourceCatalogService(db);

        var updated = await service.UpdateLearningResourceAsync(resource.LearningResourceId,
            new UpdateContentLearningResourceRequestDto
            {
                Title = "Updated title",
                Url = "https://example.com/course",
                ResourceType = "article",
                DifficultyLevel = "intermediate",
                Provider = "Updated provider",
            }, CancellationToken.None);

        Assert.Equal("Updated title", updated.Title);
        Assert.Equal("article", updated.ResourceType);
        Assert.Equal("intermediate", updated.DifficultyLevel);
    }
}
