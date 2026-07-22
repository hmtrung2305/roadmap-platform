using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Api.Controllers.LearningModules;
using RoadmapPlatform.Application.DTOs.LearningModules;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services.LearningModules;
using RoadmapPlatform.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Http.Metadata;

namespace RoadmapPlatform.Tests.LearningModules.Lessons;

public sealed class LessonAuthoringTests
{
    [Fact]
    [Trait("TestCaseId", "TC179")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Lesson Authoring")]
    [Trait("TestType", "Integration")]
    public async Task TC179_BulkUploadValidMarkdown_ShouldCreateLessonsInDeterministicOrder()
    {
        var ownerId = Guid.NewGuid();
        await using var context = Tester4TestSupport.CreateContext();
        var storage = new FakeFileStorage();
        var module = await SeedDraftModuleAsync(context, ownerId);
        var service = new LearningModuleLessonService(context, storage);

        var files = new List<LearningModuleUploadedFileDto>
        {
            UploadedFile("third.md", "# Third lesson"),
            UploadedFile("first.md", "# First lesson"),
            UploadedFile("second.md", "# Second lesson")
        };
        var request = new BulkUploadLessonsRequestDto
        {
            Lessons =
            [
                LessonItem("third", "third.md", "Third", 3),
                LessonItem("first", "first.md", "First", 1),
                LessonItem("second", "second.md", "Second", 2)
            ]
        };

        var result = await service.BulkUploadLessonsAsync(
            ownerId,
            module.SkillModuleId,
            request,
            files,
            CancellationToken.None);

        Assert.Empty(result.FailedLessons);
        Assert.Equal(3, result.Lessons.Count);
        var saved = await context.SkillModuleLessons
            .Where(item => item.SkillModuleId == module.SkillModuleId)
            .OrderBy(item => item.OrderIndex)
            .ToListAsync();
        Assert.Equal(new[] { "First", "Second", "Third" }, saved.Select(item => item.Title).ToArray());
        Assert.All(saved, item => Assert.Equal(LearningModuleLessonIndexingStatusValues.Pending, item.IndexingStatus));
        Assert.All(saved, item => Assert.False(string.IsNullOrWhiteSpace(item.MarkdownFileKey)));
    }

    [Fact]
    [Trait("TestCaseId", "TC180")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Lesson Authoring")]
    [Trait("TestType", "ApiContract")]
    public void TC180_BulkUploadEndpoint_ShouldRejectRequestsOverOneHundredMegabytes()
    {
        // Arrange
        var method = typeof(ContentManagerLearningModuleLessonsController)
            .GetMethod(
                "BulkUploadLessons",
                BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);

        var limit = method.GetCustomAttribute<RequestSizeLimitAttribute>();

        Assert.NotNull(limit);

        var metadata =
            Assert.IsAssignableFrom<
                Microsoft.AspNetCore.Http.Metadata.IRequestSizeLimitMetadata>(
                limit);

        // Assert
        Assert.Equal(
            100_000_000L,
            metadata.MaxRequestBodySize);
    }

    [Fact]
    [Trait("TestCaseId", "TC181")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Lesson Authoring")]
    [Trait("TestType", "Integration")]
    public async Task TC181_ReplaceValidMarkdown_ShouldPersistContentVersionAndQueueReindex()
    {
        var ownerId = Guid.NewGuid();
        await using var context = Tester4TestSupport.CreateContext();
        var storage = new FakeFileStorage();
        var module = await SeedDraftModuleAsync(context, ownerId);
        var lesson = Tester4TestSupport.CreateLesson(module.SkillModuleId, 1, "old/lesson.md");
        context.SkillModuleLessons.Add(lesson);
        storage.Seed("old/lesson.md", "# Old content");
        await context.SaveChangesAsync();

        var result = await new LearningModuleLessonService(context, storage).ReplaceLessonContentAsync(
            ownerId,
            module.SkillModuleId,
            lesson.SkillModuleLessonId,
            UploadedFile("replacement.md", "# Replacement content\nUpdated details."),
            CancellationToken.None);

        Assert.Equal(2, result.ContentVersion);
        Assert.Equal(LearningModuleLessonIndexingStatusValues.Pending, result.IndexingStatus);
        Assert.Null(result.IndexedAt);
        Assert.Contains("old/lesson.md", storage.DeletedObjectPaths);
        Assert.NotEqual("old/lesson.md", result.MarkdownFileKey);
    }

    [Fact]
    [Trait("TestCaseId", "TC182")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Lesson Authoring")]
    [Trait("TestType", "ApiContract")]
    public void TC182_ReplacementEndpoint_ShouldRejectRequestsOverFiftyMegabytes()
    {
        // Arrange
        var method = typeof(ContentManagerLearningModuleLessonsController)
            .GetMethod(
                "ReplaceLessonMarkdown",
                BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);

        var limit = method.GetCustomAttribute<RequestSizeLimitAttribute>();

        Assert.NotNull(limit);

        var metadata =
            Assert.IsAssignableFrom<
                Microsoft.AspNetCore.Http.Metadata.IRequestSizeLimitMetadata>(
                limit);

        // Assert
        Assert.Equal(
            50_000_000L,
            metadata.MaxRequestBodySize);
    }

    [Fact]
    [Trait("TestCaseId", "TC183")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Lesson Authoring")]
    [Trait("TestType", "Integration")]
    public async Task TC183_EditLessonMetadata_ShouldPreserveStoredContent()
    {
        var ownerId = Guid.NewGuid();
        await using var context = Tester4TestSupport.CreateContext();
        var module = await SeedDraftModuleAsync(context, ownerId);
        var lesson = Tester4TestSupport.CreateLesson(module.SkillModuleId, 1, "content/original.md");
        context.SkillModuleLessons.Add(lesson);
        await context.SaveChangesAsync();

        var result = await new LearningModuleLessonService(context, new FakeFileStorage()).UpdateLessonAsync(
            ownerId,
            module.SkillModuleId,
            lesson.SkillModuleLessonId,
            new UpdateLearningModuleLessonRequestDto
            {
                Title = "Updated lesson",
                Slug = "updated-lesson",
                Summary = "Updated summary",
                EstimatedHours = 2
            },
            CancellationToken.None);

        Assert.Equal("Updated lesson", result.Title);
        Assert.Equal("updated-lesson", result.Slug);
        Assert.Equal("Updated summary", result.Summary);
        Assert.Equal(2m, result.EstimatedHours);
        Assert.Equal("content/original.md", result.MarkdownFileKey);
        Assert.Equal(1, result.ContentVersion);
    }

    [Fact]
    [Trait("TestCaseId", "TC184")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Lesson Authoring")]
    [Trait("TestType", "Integration")]
    public async Task TC184_ReorderLessons_ShouldPersistCompleteConsecutiveOrder()
    {
        var ownerId = Guid.NewGuid();
        await using var context = Tester4TestSupport.CreateContext();
        var module = await SeedDraftModuleAsync(context, ownerId);
        var first = Tester4TestSupport.CreateLesson(module.SkillModuleId, 1);
        var second = Tester4TestSupport.CreateLesson(module.SkillModuleId, 2);
        var third = Tester4TestSupport.CreateLesson(module.SkillModuleId, 3);
        context.SkillModuleLessons.AddRange(first, second, third);
        await context.SaveChangesAsync();

        var result = await new LearningModuleLessonService(context, new FakeFileStorage()).ReorderLessonsAsync(
            ownerId,
            module.SkillModuleId,
            new ReorderLessonsRequestDto
            {
                Lessons =
                [
                    new() { SkillModuleLessonId = third.SkillModuleLessonId, OrderIndex = 1 },
                    new() { SkillModuleLessonId = first.SkillModuleLessonId, OrderIndex = 2 },
                    new() { SkillModuleLessonId = second.SkillModuleLessonId, OrderIndex = 3 }
                ]
            },
            CancellationToken.None);

        Assert.Equal(
            new[] { third.SkillModuleLessonId, first.SkillModuleLessonId, second.SkillModuleLessonId },
            result.Select(item => item.SkillModuleLessonId).ToArray());
        Assert.Equal(new[] { 1, 2, 3 }, result.Select(item => item.OrderIndex).ToArray());
    }

    [Fact]
    [Trait("TestCaseId", "TC185")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Lesson Authoring")]
    [Trait("TestType", "Validation")]
    public async Task TC185_DuplicateOrMissingLessonIds_ShouldRejectReorderAndPreserveOriginalOrder()
    {
        var ownerId = Guid.NewGuid();
        await using var context = Tester4TestSupport.CreateContext();
        var module = await SeedDraftModuleAsync(context, ownerId);
        var first = Tester4TestSupport.CreateLesson(module.SkillModuleId, 1);
        var second = Tester4TestSupport.CreateLesson(module.SkillModuleId, 2);
        context.SkillModuleLessons.AddRange(first, second);
        await context.SaveChangesAsync();

        var service = new LearningModuleLessonService(context, new FakeFileStorage());
        await Assert.ThrowsAsync<ConflictException>(() => service.ReorderLessonsAsync(
            ownerId,
            module.SkillModuleId,
            new ReorderLessonsRequestDto
            {
                Lessons =
                [
                    new() { SkillModuleLessonId = first.SkillModuleLessonId, OrderIndex = 1 },
                    new() { SkillModuleLessonId = first.SkillModuleLessonId, OrderIndex = 2 }
                ]
            },
            CancellationToken.None));

        context.ChangeTracker.Clear();
        var saved = await context.SkillModuleLessons.OrderBy(item => item.OrderIndex).ToListAsync();
        Assert.Equal(new[] { first.SkillModuleLessonId, second.SkillModuleLessonId }, saved.Select(item => item.SkillModuleLessonId).ToArray());
    }

    [Fact]
    [Trait("TestCaseId", "TC186")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Lesson Authoring")]
    [Trait("TestType", "SourceContract")]
    public void TC186_DeleteDraftLesson_ShouldRemoveChunksLessonAndStoredFile()
    {
        var source = Tester4TestSupport.ReadRepositoryFile(
            "src", "backend", "RoadmapPlatform.Infrastructure", "Services", "LearningModules", "LearningModuleLessonService.cs");
        var methodStart = source.IndexOf("public async Task DeleteDraftLessonAsync", StringComparison.Ordinal);
        var chunksIndex = source.IndexOf("ExecuteDeleteAsync", methodStart, StringComparison.Ordinal);
        var lessonIndex = source.IndexOf("_context.SkillModuleLessons.Remove(lesson);", methodStart, StringComparison.Ordinal);
        var saveIndex = source.IndexOf("await _context.SaveChangesAsync", methodStart, StringComparison.Ordinal);
        var fileIndex = source.IndexOf("await TryDeleteStoredFileAsync(fileKey);", methodStart, StringComparison.Ordinal);

        Assert.True(methodStart >= 0);
        Assert.True(chunksIndex > methodStart);
        Assert.True(lessonIndex > chunksIndex);
        Assert.True(saveIndex > lessonIndex);
        Assert.True(fileIndex > saveIndex);
    }

    [Fact]
    [Trait("TestCaseId", "TC187")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Lesson Authoring")]
    [Trait("TestType", "Integration")]
    public async Task TC187_ReindexCommand_ShouldQueueLessonAndWorkerShouldOwnIndexedCompletion()
    {
        var ownerId = Guid.NewGuid();
        await using var context = Tester4TestSupport.CreateContext();
        var module = await SeedDraftModuleAsync(context, ownerId);
        var lesson = Tester4TestSupport.CreateLesson(
            module.SkillModuleId,
            1,
            indexingStatus: LearningModuleLessonIndexingStatusValues.Failed);
        lesson.IndexingError = "stale index";
        context.SkillModuleLessons.Add(lesson);
        await context.SaveChangesAsync();

        var result = await new LearningModuleLessonService(context, new FakeFileStorage()).ReindexLessonAsync(
            ownerId,
            module.SkillModuleId,
            lesson.SkillModuleLessonId,
            CancellationToken.None);

        Assert.Equal(LearningModuleLessonIndexingStatusValues.Pending, result.IndexingStatus);
        Assert.Null(result.IndexingError);

        var workerSource = Tester4TestSupport.ReadRepositoryFile(
            "src", "backend", "RoadmapPlatform.Infrastructure",
            "Services", "LearningModules",
            "LearningModuleIndexingWorker.cs");

        Assert.Contains(
            "indexingService.IndexLessonAsync",
            workerSource,
            StringComparison.Ordinal);

        Assert.Contains(
            "LearningModuleLessonIndexingStatusValues.Indexed",
            workerSource,
            StringComparison.Ordinal);

        var indexingServiceSource = Tester4TestSupport.ReadRepositoryFile(
            "src", "backend", "RoadmapPlatform.Infrastructure",
            "Services", "LearningModules",
            "LearningModuleRagIndexingService.cs");

        Assert.Contains(
            "_context.SkillModuleChunks.AddRange",
            indexingServiceSource,
            StringComparison.Ordinal);
    }

    private static async Task<SkillModule> SeedDraftModuleAsync(
        RoadmapPlatform.Infrastructure.Data.ApplicationDbContext context,
        Guid ownerId)
    {
        var skill = Tester4TestSupport.CreateSkill();
        var module = Tester4TestSupport.CreateModule(ownerId, skill.SkillId, LearningModuleStatusValues.Draft);
        context.Skills.Add(skill);
        context.SkillModules.Add(module);
        await context.SaveChangesAsync();
        return module;
    }

    private static LearningModuleUploadedFileDto UploadedFile(string fileName, string markdown)
    {
        var bytes = Encoding.UTF8.GetBytes(markdown);
        return new LearningModuleUploadedFileDto
        {
            FileName = fileName,
            ContentType = "text/markdown",
            Length = bytes.LongLength,
            Content = new MemoryStream(bytes)
        };
    }

    private static BulkUploadLessonItemDto LessonItem(
        string clientId,
        string fileName,
        string title,
        int orderIndex)
    {
        return new BulkUploadLessonItemDto
        {
            ClientId = clientId,
            FileName = fileName,
            Title = title,
            OrderIndex = orderIndex,
            EstimatedHours = 1
        };
    }
}
