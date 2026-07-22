using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.LearningResources;
using RoadmapPlatform.Application.Exceptions;

namespace RoadmapPlatform.Tests.LearningModules.Resources;

public sealed class LearningResourcesTests
{
    [Fact]
    public async Task TC091_CreateLearningResource_WithValidInput_ShouldCreateAndAppearInSearch()
    {
        await using var fixture = await LearningResourcesTestFixture.CreateAsync();
        var request = LearningResourcesTestFixture.CreateValidRequest();

        var created = await fixture.Service.CreateLearningResourceAsync(
            request,
            CancellationToken.None);

        fixture.DbContext.ChangeTracker.Clear();

        var persisted = await fixture.DbContext.LearningResources
            .AsNoTracking()
            .SingleAsync(resource => resource.LearningResourceId == created.ResourceId);

        var search = await fixture.Service.SearchLearningResourcesAsync(
            new ContentLearningResourceSearchQueryDto
            {
                Search = request.Title,
                ResourceType = request.ResourceType,
                DifficultyLevel = request.DifficultyLevel
            },
            CancellationToken.None);

        Assert.Equal(request.Title, created.Title);
        Assert.Equal(request.Url, created.Url);
        Assert.Equal("documentation", created.ResourceType);
        Assert.Equal("intermediate", created.DifficultyLevel);

        Assert.Equal(created.ResourceId, persisted.LearningResourceId);
        Assert.Equal(request.Title, persisted.Title);
        Assert.Equal(request.Url, persisted.Url);

        Assert.Contains(search.Items, item =>
            item.ResourceId == created.ResourceId
            && item.Title == request.Title
            && item.Url == request.Url);
    }

    [Fact]
    public async Task TC092_CreateLearningResource_WhenUrlAlreadyExists_ShouldRejectWithUniquenessMessage()
    {
        await using var fixture = await LearningResourcesTestFixture.CreateAsync();
        const string existingUrl = "https://example.com/courses/backend-fundamentals";
        await fixture.SeedResourceAsync(existingUrl);

        var request = LearningResourcesTestFixture.CreateValidRequest(
            "  HTTPS://EXAMPLE.COM/COURSES/BACKEND-FUNDAMENTALS  ",
            "Duplicate Resource");

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            fixture.Service.CreateLearningResourceAsync(request, CancellationToken.None));

        Assert.Equal("A learning resource with this URL already exists.", exception.Message);
        Assert.Equal(1, await fixture.CountResourcesAsync());
    }

    [Fact]
    public async Task TC093_CreateLearningResource_WithFtpUrl_ShouldRejectAndCreateNothing()
    {
        await using var fixture = await LearningResourcesTestFixture.CreateAsync();
        var request = LearningResourcesTestFixture.CreateValidRequest(
            "ftp://example.com/course",
            "FTP Course");

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            fixture.Service.CreateLearningResourceAsync(request, CancellationToken.None));

        Assert.Equal(
            "Learning resource URL must be a valid HTTP or HTTPS URL.",
            exception.Message);
        Assert.Equal(0, await fixture.CountResourcesAsync());
    }

    [Fact]
    public async Task TC094_CreateLearningResource_WithUnsupportedResourceType_ShouldReject()
    {
        await using var fixture = await LearningResourcesTestFixture.CreateAsync();
        var request = LearningResourcesTestFixture.CreateValidRequest();
        request.ResourceType = "podcast-series";

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            fixture.Service.CreateLearningResourceAsync(request, CancellationToken.None));

        Assert.Equal("Resource type is invalid.", exception.Message);
        Assert.Equal(0, await fixture.CountResourcesAsync());
    }

    [Fact]
    public async Task TC095_CreateLearningResource_WithUnsupportedDifficulty_ShouldReject()
    {
        await using var fixture = await LearningResourcesTestFixture.CreateAsync();
        var request = LearningResourcesTestFixture.CreateValidRequest();
        request.DifficultyLevel = "expert";

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            fixture.Service.CreateLearningResourceAsync(request, CancellationToken.None));

        Assert.Equal("Difficulty level is invalid.", exception.Message);
        Assert.Equal(0, await fixture.CountResourcesAsync());
    }

    [Fact]
    public async Task TC096_UpdateLearningResource_WithValidChangedMetadata_ShouldReturnChangesInDetailAndSearch()
    {
        await using var fixture = await LearningResourcesTestFixture.CreateAsync();
        var created = await fixture.Service.CreateLearningResourceAsync(
            LearningResourcesTestFixture.CreateValidRequest(),
            CancellationToken.None);

        var updateRequest = new UpdateContentLearningResourceRequestDto
        {
            Title = "Updated ASP.NET Core Course",
            Url = $"https://example.com/resources/updated-{Guid.NewGuid():N}",
            ResourceType = "course",
            Description = "Updated learning resource description.",
            Provider = "Updated Academy",
            DifficultyLevel = "advanced"
        };

        var updated = await fixture.Service.UpdateLearningResourceAsync(
            created.ResourceId,
            updateRequest,
            CancellationToken.None);

        var detail = await fixture.Service.GetLearningResourceAsync(
            created.ResourceId,
            CancellationToken.None);

        var search = await fixture.Service.SearchLearningResourcesAsync(
            new ContentLearningResourceSearchQueryDto
            {
                Search = "Updated ASP.NET Core",
                ResourceType = "course",
                DifficultyLevel = "advanced"
            },
            CancellationToken.None);

        Assert.Equal(created.ResourceId, updated.ResourceId);
        Assert.Equal(updateRequest.Title, detail.Title);
        Assert.Equal(updateRequest.Url, detail.Url);
        Assert.Equal("course", detail.ResourceType);
        Assert.Equal(updateRequest.Description, detail.Description);
        Assert.Equal(updateRequest.Provider, detail.Provider);
        Assert.Equal("advanced", detail.DifficultyLevel);
        Assert.Contains(search.Items, item =>
            item.ResourceId == created.ResourceId
            && item.Title == updateRequest.Title
            && item.Url == updateRequest.Url);
        Assert.Equal(1, await fixture.CountResourcesAsync());
    }
}
