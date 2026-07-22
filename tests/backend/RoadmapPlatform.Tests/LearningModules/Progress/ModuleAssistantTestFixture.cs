using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RoadmapPlatform.Application.DTOs.AiCredits;
using RoadmapPlatform.Application.DTOs.LearningModules;
using RoadmapPlatform.Application.Interfaces.AiCredits;
using RoadmapPlatform.Application.Interfaces.LearningModules;
using RoadmapPlatform.Infrastructure.Configurations;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services.LearningModules;

namespace RoadmapPlatform.Tests.LearningModules.Progress;

internal sealed class ModuleAssistantTestFixture : IAsyncDisposable
{
    private ModuleAssistantTestFixture(
        ApplicationDbContext context,
        LearningModuleChatService service,
        StubLearningModuleRagIndexingService ragIndexingService,
        InterceptingAiCreditService aiCreditService,
        Guid userId,
        Guid moduleId,
        Guid currentLessonId,
        Guid otherLessonId,
        Guid currentLessonChunkId,
        Guid otherLessonChunkId,
        string currentLessonContent,
        string otherLessonContent)
    {
        Context = context;
        Service = service;
        RagIndexingService = ragIndexingService;
        AiCreditService = aiCreditService;
        UserId = userId;
        ModuleId = moduleId;
        CurrentLessonId = currentLessonId;
        OtherLessonId = otherLessonId;
        CurrentLessonChunkId = currentLessonChunkId;
        OtherLessonChunkId = otherLessonChunkId;
        CurrentLessonContent = currentLessonContent;
        OtherLessonContent = otherLessonContent;
    }

    public ApplicationDbContext Context { get; }

    public LearningModuleChatService Service { get; }

    public StubLearningModuleRagIndexingService RagIndexingService { get; }

    public InterceptingAiCreditService AiCreditService { get; }

    public Guid UserId { get; }

    public Guid ModuleId { get; }

    public Guid CurrentLessonId { get; }

    public Guid OtherLessonId { get; }

    public Guid CurrentLessonChunkId { get; }

    public Guid OtherLessonChunkId { get; }

    public string CurrentLessonContent { get; }

    public string OtherLessonContent { get; }

    public SkillModuleLesson CurrentLesson => new()
    {
        SkillModuleLessonId = CurrentLessonId,
        SkillModuleId = ModuleId,
        Title = "Dependency Injection Basics",
        Slug = "dependency-injection-basics",
        OrderIndex = 1,
        MarkdownFileKey = $"modules/{ModuleId}/lesson-1.md",
        ContentVersion = 1,
        IndexingStatus = LearningModuleLessonIndexingStatusValues.Indexed
    };

    public static async Task<ModuleAssistantTestFixture> CreateAsync(
        bool enrolled = true)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        var context = new TestApplicationDbContext(options);
        var ragIndexingService = new StubLearningModuleRagIndexingService();
        var aiCreditService = new InterceptingAiCreditService(initialCredits: 5);
        var service = new LearningModuleChatService(
            context,
            ragIndexingService,
            aiCreditService,
            Options.Create(new AiSettings
            {
                ApiKey = "unit-test-placeholder",
                GenerationModel = "gemini-2.5-flash"
            }),
            Options.Create(new LearningModuleRagSettings
            {
                MaxChunks = 5
            }));

        var now = DateTime.UtcNow;
        var userId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var moduleId = Guid.NewGuid();
        var currentLessonId = Guid.NewGuid();
        var otherLessonId = Guid.NewGuid();
        var currentLessonChunkId = Guid.NewGuid();
        var otherLessonChunkId = Guid.NewGuid();
        var currentLessonContent =
            "Dependency injection supplies dependencies from outside the class.";
        var otherLessonContent =
            "Constructor injection makes dependencies explicit and improves testability.";

        context.AddRange(
            new User
            {
                UserId = userId,
                Username = "module-assistant-learner",
                UsernameNormalized = "MODULE-ASSISTANT-LEARNER",
                Status = "active",
                CreatedAt = now,
                UpdatedAt = now
            },
            new Skill
            {
                SkillId = skillId,
                Name = "Dependency Injection",
                Slug = $"dependency-injection-{Guid.NewGuid():N}",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new SkillModule
            {
                SkillModuleId = moduleId,
                SkillId = skillId,
                Title = "Dependency Injection Module",
                Slug = $"dependency-injection-module-{Guid.NewGuid():N}",
                Status = LearningModuleStatusValues.Published,
                PublishedAt = now,
                Metadata = "{}",
                CreatedAt = now,
                UpdatedAt = now
            },
            new SkillModuleLesson
            {
                SkillModuleLessonId = currentLessonId,
                SkillModuleId = moduleId,
                Title = "Dependency Injection Basics",
                Slug = "dependency-injection-basics",
                OrderIndex = 1,
                MarkdownFileKey = $"modules/{moduleId}/lesson-1.md",
                ContentVersion = 1,
                IndexingStatus = LearningModuleLessonIndexingStatusValues.Indexed,
                IndexedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            },
            new SkillModuleLesson
            {
                SkillModuleLessonId = otherLessonId,
                SkillModuleId = moduleId,
                Title = "Constructor Injection",
                Slug = "constructor-injection",
                OrderIndex = 2,
                MarkdownFileKey = $"modules/{moduleId}/lesson-2.md",
                ContentVersion = 1,
                IndexingStatus = LearningModuleLessonIndexingStatusValues.Indexed,
                IndexedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            },
            new SkillModuleChunk
            {
                SkillModuleChunkId = currentLessonChunkId,
                SkillModuleId = moduleId,
                SkillModuleLessonId = currentLessonId,
                ChunkIndex = 0,
                Heading = "Core concept",
                Content = currentLessonContent,
                ContentHash = Guid.NewGuid().ToString("N"),
                CreatedAt = now
            },
            new SkillModuleChunk
            {
                SkillModuleChunkId = otherLessonChunkId,
                SkillModuleId = moduleId,
                SkillModuleLessonId = otherLessonId,
                ChunkIndex = 0,
                Heading = "Testing benefit",
                Content = otherLessonContent,
                ContentHash = Guid.NewGuid().ToString("N"),
                CreatedAt = now
            });

        if (enrolled)
        {
            context.SkillModuleEnrollments.Add(new SkillModuleEnrollment
            {
                SkillModuleEnrollmentId = Guid.NewGuid(),
                UserId = userId,
                SkillModuleId = moduleId,
                Status = LearningModuleEnrollmentStatusValues.InProgress,
                StartedAt = now,
                LastAccessedLessonId = currentLessonId,
                ProgressPercent = 0,
                LessonProgress = "{}",
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        return new ModuleAssistantTestFixture(
            context,
            service,
            ragIndexingService,
            aiCreditService,
            userId,
            moduleId,
            currentLessonId,
            otherLessonId,
            currentLessonChunkId,
            otherLessonChunkId,
            currentLessonContent,
            otherLessonContent);
    }

    public LearningModuleRagSourceDto CreateOtherLessonSource()
    {
        return new LearningModuleRagSourceDto
        {
            SkillModuleChunkId = OtherLessonChunkId,
            SkillModuleLessonId = OtherLessonId,
            LessonTitle = "Constructor Injection",
            Heading = "Testing benefit",
            ContentPreview = OtherLessonContent,
            SimilarityScore = 0.92
        };
    }

    public ValueTask DisposeAsync() => Context.DisposeAsync();

    internal sealed class StubLearningModuleRagIndexingService
        : ILearningModuleRagIndexingService
    {
        public IReadOnlyList<LearningModuleRagSourceDto> SearchResults { get; set; } = [];

        public int SearchCallCount { get; private set; }

        public Guid? LastSkillModuleId { get; private set; }

        public Guid? LastPreferredLessonId { get; private set; }

        public string? LastQuery { get; private set; }

        public int? LastLimit { get; private set; }

        public Task<IReadOnlyList<LearningModuleChunkDto>> IndexLessonAsync(
            Guid skillModuleId,
            Guid skillModuleLessonId,
            string markdown,
            int expectedContentVersion,
            string? expectedContentHash,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task DeleteLessonChunksAsync(
            Guid skillModuleLessonId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<LearningModuleRagSourceDto>> SearchRelevantChunksAsync(
            Guid skillModuleId,
            Guid? preferredLessonId,
            string query,
            int limit,
            CancellationToken cancellationToken)
        {
            SearchCallCount++;
            LastSkillModuleId = skillModuleId;
            LastPreferredLessonId = preferredLessonId;
            LastQuery = query;
            LastLimit = limit;
            return Task.FromResult(SearchResults);
        }
    }

    internal sealed class InterceptingAiCreditService : IAiCreditService
    {
        public InterceptingAiCreditService(int initialCredits)
        {
            RemainingCredits = initialCredits;
        }

        public int RemainingCredits { get; private set; }

        public int SpendCallCount { get; private set; }

        public Guid? LastUserId { get; private set; }

        public string? LastFeatureName { get; private set; }

        public int? LastCreditCost { get; private set; }

        public Guid? LastRequestRefId { get; private set; }

        public bool ThrowAfterSpend { get; set; } = true;

        public Task<AiCreditStatusDto> GetStatusAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(BuildStatus());

        public Task<AiCreditStatusDto> SpendAsync(
            Guid userId,
            string featureName,
            int creditCost,
            Guid? requestRefId = null,
            string? metadata = null,
            CancellationToken cancellationToken = default)
        {
            SpendCallCount++;
            LastUserId = userId;
            LastFeatureName = featureName;
            LastCreditCost = creditCost;
            LastRequestRefId = requestRefId;
            RemainingCredits -= creditCost;

            if (ThrowAfterSpend)
            {
                throw new CreditSpendInterceptedException();
            }

            return Task.FromResult(BuildStatus());
        }

        private AiCreditStatusDto BuildStatus()
        {
            return new AiCreditStatusDto
            {
                PlanCode = "test",
                DailyCreditLimit = 5,
                UsedCreditsToday = 5 - RemainingCredits,
                RemainingCreditsToday = RemainingCredits,
                ResetAt = DateTimeOffset.UtcNow.AddDays(1)
            };
        }
    }

    internal sealed class CreditSpendInterceptedException : Exception
    {
    }

    private sealed class TestApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<SkillModuleChunk>()
                .Ignore(chunk => chunk.Embedding);
        }
    }
}
