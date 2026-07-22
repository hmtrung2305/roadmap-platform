using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using RoadmapPlatform.Application.DTOs.SkillGapAnalysis.CategoryConfig;
using RoadmapPlatform.Application.Interfaces.SkillGapAnalysis;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services.Roadmaps;
using RoadmapPlatform.Infrastructure.Services.Roadmaps.ContentManagement;

namespace RoadmapPlatform.Tests.Roadmaps;

internal sealed class RoadmapTestFixture : IAsyncDisposable
{
    private readonly SqliteConnection? sqliteConnection;

    private RoadmapTestFixture(ApplicationDbContext context, SqliteConnection? sqliteConnection = null)
    {
        Context = context;
        this.sqliteConnection = sqliteConnection;
        QueryService = new RoadmapQueryService(context, new RoadmapDetailBuilder(context));
        ContentQueryService = new ContentManagerRoadmapQueryService(context);
        ValidationService = new ContentManagerRoadmapValidationService(context);
        SkillGapConfigService = new StubSkillGapCategoryConfigService();
        DraftService = new ContentManagerRoadmapDraftService(
            context,
            ContentQueryService,
            ValidationService,
            SkillGapConfigService);
        StructureService = new ContentManagerRoadmapStructureService(context, ContentQueryService);
        MetadataService = new ContentManagerRoadmapMetadataService(context, ContentQueryService);
        MappingService = new ContentManagerRoadmapMappingService(context, ContentQueryService);
        EnrollmentService = new RoadmapEnrollmentService(context);
        ProgressService = new RoadmapProgressService(context);
    }

    public ApplicationDbContext Context { get; }

    public RoadmapQueryService QueryService { get; }

    public ContentManagerRoadmapQueryService ContentQueryService { get; }

    public ContentManagerRoadmapValidationService ValidationService { get; }

    public ContentManagerRoadmapDraftService DraftService { get; }

    public ContentManagerRoadmapStructureService StructureService { get; }

    public ContentManagerRoadmapMetadataService MetadataService { get; }

    public ContentManagerRoadmapMappingService MappingService { get; }

    public RoadmapEnrollmentService EnrollmentService { get; }

    public RoadmapProgressService ProgressService { get; }

    public StubSkillGapCategoryConfigService SkillGapConfigService { get; }

    public User Owner { get; private set; } = null!;

    public User Learner { get; private set; } = null!;

    public User OtherLearner { get; private set; } = null!;

    public User Reviewer { get; private set; } = null!;

    public CareerRole CareerRole { get; private set; } = null!;

    public static async Task<RoadmapTestFixture> CreateAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var fixture = new RoadmapTestFixture(new TestApplicationDbContext(options));
        await SeedCoreDataAsync(fixture);
        return fixture;
    }

    public static async Task<RoadmapTestFixture> CreateRelationalAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        var fixture = new RoadmapTestFixture(
            new RelationalTestApplicationDbContext(options),
            connection);
        await fixture.Context.Database.EnsureCreatedAsync();
        await fixture.Context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");
        await SeedCoreDataAsync(fixture);
        return fixture;
    }

    private static async Task SeedCoreDataAsync(RoadmapTestFixture fixture)
    {
        var now = DateTime.UtcNow;

        fixture.Owner = fixture.CreateUser("roadmap-author", "Roadmap Author", now);
        fixture.Learner = fixture.CreateUser("learner-one", "Learner One", now);
        fixture.OtherLearner = fixture.CreateUser("learner-two", "Learner Two", now);
        fixture.Reviewer = fixture.CreateUser("roadmap-reviewer", "Roadmap Reviewer", now);
        fixture.CareerRole = new CareerRole
        {
            CareerRoleId = Guid.NewGuid(),
            Name = "Software Engineer",
            Slug = "software-engineer",
            Description = "Builds software systems.",
            Category = "Engineering",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        fixture.Context.AddRange(
            fixture.Owner,
            fixture.Learner,
            fixture.OtherLearner,
            fixture.Reviewer,
            fixture.CareerRole);
        await fixture.Context.SaveChangesAsync();
    }

    public Roadmap CreateRoadmap(
        string title = "Software Engineering Roadmap",
        string visibility = "public",
        User? owner = null,
        CareerRole? careerRole = null)
    {
        var now = DateTime.UtcNow;
        owner ??= Owner;
        careerRole ??= CareerRole;
        var slug = title.ToLowerInvariant().Replace(' ', '-');
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var roadmap = new Roadmap
        {
            RoadmapId = Guid.NewGuid(),
            CareerRoleId = careerRole.CareerRoleId,
            CareerRole = careerRole,
            OwnerUserId = owner.UserId,
            OwnerUser = owner,
            Title = title,
            Slug = $"{slug}-{suffix}",
            Description = $"{title} description",
            Visibility = visibility,
            CreatedAt = now,
            UpdatedAt = now,
        };

        Context.Add(roadmap);
        return roadmap;
    }

    public RoadmapVersion AddVersion(
        Roadmap roadmap,
        string status = "draft",
        int major = 1,
        int minor = 0,
        int patch = 0,
        string? releaseType = null,
        Guid? createdFromVersionId = null,
        string? title = null,
        User? createdBy = null)
    {
        var now = DateTime.UtcNow;
        createdBy ??= Owner;
        releaseType ??= major == 1 && minor == 0 && patch == 0 && createdFromVersionId == null
            ? "initial"
            : patch > 0 ? "patch" : minor > 0 ? "minor" : "major";
        var version = new RoadmapVersion
        {
            RoadmapVersionId = Guid.NewGuid(),
            RoadmapId = roadmap.RoadmapId,
            Roadmap = roadmap,
            VersionNumber = major * 10000 + minor * 100 + patch,
            MajorVersion = major,
            MinorVersion = minor,
            PatchVersion = patch,
            ReleaseType = releaseType,
            Status = status,
            Title = title ?? roadmap.Title,
            Description = roadmap.Description,
            EstimatedTotalHours = 24,
            LayoutDirection = "TB",
            LayoutAlgorithm = null,
            CreatedFromVersionId = createdFromVersionId,
            CreatedByUserId = createdBy.UserId,
            CreatedByUser = createdBy,
            PublishedAt = status == "published" ? now : null,
            CreatedAt = now,
            UpdatedAt = now,
        };

        roadmap.RoadmapVersions.Add(version);
        Context.Add(version);
        return version;
    }

    public (RoadmapNode Phase, RoadmapNode Project) AddValidGraph(
        RoadmapVersion version,
        string projectSlug = "build-project",
        bool projectRequired = true,
        string projectTitle = "Build a project")
    {
        var phase = AddNode(
            version,
            nodeType: "phase",
            slug: "foundation-phase",
            title: "Foundation",
            parent: null,
            isRequired: true,
            isTrackable: false,
            orderIndex: 1);
        var project = AddNode(
            version,
            nodeType: "project",
            slug: projectSlug,
            title: projectTitle,
            parent: phase,
            isRequired: projectRequired,
            isTrackable: true,
            orderIndex: 1);
        return (phase, project);
    }

    public RoadmapNode AddNode(
        RoadmapVersion version,
        string nodeType,
        string slug,
        string title,
        RoadmapNode? parent = null,
        bool isRequired = true,
        bool? isTrackable = null,
        int orderIndex = 1,
        string? description = "Useful learner-facing content.")
    {
        var normalizedType = nodeType == "group" ? "choice_group" : nodeType;
        var trackable = isTrackable ?? normalizedType is "topic" or "choice_option" or "checkpoint" or "project";
        var node = new RoadmapNode
        {
            RoadmapNodeId = Guid.NewGuid(),
            RoadmapVersionId = version.RoadmapVersionId,
            RoadmapVersion = version,
            ParentNodeId = parent?.RoadmapNodeId,
            ParentNode = parent,
            Slug = slug,
            NodeType = normalizedType,
            CheckpointType = normalizedType == "checkpoint" ? "assessment" : null,
            SelectionType = normalizedType == "choice_group" ? "complete_all" : null,
            RequiredCount = null,
            Title = title,
            Description = description,
            OrderIndex = orderIndex,
            LayoutRole = normalizedType == "phase" ? "trunk" : trackable ? "skill" : "branch",
            EstimatedHours = trackable ? 2 : null,
            DifficultyLevel = trackable ? "beginner" : null,
            Metadata = "{}",
            IsRequired = isRequired,
            IsTrackable = trackable,
            LearningOutcomes = trackable ? "[\"Apply the concept\"]" : "[]",
            CompletionCriteria = trackable ? "[\"Complete the work\"]" : "[]",
            CreatedAt = DateTime.UtcNow,
        };

        version.RoadmapNodes.Add(node);
        parent?.InverseParentNode.Add(node);
        Context.Add(node);

        if (parent != null)
        {
            AddEdge(
                version,
                parent,
                node,
                parent.NodeType == "choice_group" ? "choice" : "contains",
                isRequired ? "required" : "optional");
        }

        return node;
    }

    public RoadmapEdge AddEdge(
        RoadmapVersion version,
        RoadmapNode from,
        RoadmapNode to,
        string edgeType,
        string dependencyType = "required")
    {
        var edge = new RoadmapEdge
        {
            RoadmapEdgeId = Guid.NewGuid(),
            RoadmapVersionId = version.RoadmapVersionId,
            RoadmapVersion = version,
            FromNodeId = from.RoadmapNodeId,
            RoadmapNode = from,
            ToNodeId = to.RoadmapNodeId,
            RoadmapNodeNavigation = to,
            EdgeType = edgeType,
            DependencyType = dependencyType,
            Condition = "{}",
        };

        version.RoadmapEdges.Add(edge);
        from.RoadmapEdgeRoadmapNodes.Add(edge);
        to.RoadmapEdgeRoadmapNodeNavigations.Add(edge);
        Context.Add(edge);
        return edge;
    }

    public RoadmapEnrollment AddEnrollment(
        RoadmapVersion version,
        User? user = null,
        decimal progressPercent = 0,
        string status = "active")
    {
        user ??= Learner;
        var enrollment = new RoadmapEnrollment
        {
            RoadmapEnrollmentId = Guid.NewGuid(),
            UserId = user.UserId,
            User = user,
            RoadmapVersionId = version.RoadmapVersionId,
            RoadmapVersion = version,
            Status = status,
            ProgressPercent = progressPercent,
            StartedAt = DateTime.UtcNow,
            CompletedAt = status == "completed" ? DateTime.UtcNow : null,
            UpdatedAt = DateTime.UtcNow,
        };

        version.RoadmapEnrollments.Add(enrollment);
        user.RoadmapEnrollments.Add(enrollment);
        Context.Add(enrollment);
        return enrollment;
    }

    public UserNodeProgress AddProgress(
        RoadmapEnrollment enrollment,
        RoadmapNode node,
        string status,
        DateTime? updatedAt = null)
    {
        var timestamp = updatedAt ?? DateTime.UtcNow;
        var progress = new UserNodeProgress
        {
            UserNodeProgressId = Guid.NewGuid(),
            RoadmapEnrollmentId = enrollment.RoadmapEnrollmentId,
            RoadmapEnrollment = enrollment,
            RoadmapNodeId = node.RoadmapNodeId,
            RoadmapNode = node,
            Status = status,
            StartedAt = status is "in_progress" or "completed" or "skipped" ? timestamp : null,
            CompletedAt = status == "completed" ? timestamp : null,
            SkippedAt = status == "skipped" ? timestamp : null,
            UpdatedAt = timestamp,
        };

        enrollment.UserNodeProgresses.Add(progress);
        node.UserNodeProgresses.Add(progress);
        Context.Add(progress);
        return progress;
    }

    public Skill AddSkill(string name = "Testing")
    {
        var skill = new Skill
        {
            SkillId = Guid.NewGuid(),
            Name = name,
            Slug = $"{name.ToLowerInvariant().Replace(' ', '-')}-{Guid.NewGuid():N}",
            Description = $"{name} skill",
            Category = "Engineering",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        Context.Add(skill);
        return skill;
    }

    public async Task SaveAsync()
    {
        await Context.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        if (sqliteConnection != null)
        {
            await sqliteConnection.DisposeAsync();
        }
    }

    private User CreateUser(string username, string displayName, DateTime now)
    {
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Username = username,
            UsernameNormalized = username.ToUpperInvariant(),
            Status = "active",
            CreatedAt = now,
            UpdatedAt = now,
        };
        var profile = new UserProfile
        {
            UserId = user.UserId,
            User = user,
            DisplayName = displayName,
            IsPublic = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        user.UserProfile = profile;
        return user;
    }

    internal sealed class TestApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<SkillModuleChunk>().Ignore(chunk => chunk.Embedding);
        }
    }

    internal sealed class RelationalTestApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<SkillModuleChunk>().Ignore(chunk => chunk.Embedding);

            foreach (var property in modelBuilder.Model
                .GetEntityTypes()
                .SelectMany(entityType => entityType.GetProperties()))
            {
                property.SetDefaultValueSql(null);
            }

            foreach (var index in modelBuilder.Model
                .GetEntityTypes()
                .SelectMany(entityType => entityType.GetIndexes()))
            {
                index.SetFilter(null);
            }
        }
    }
}

internal sealed class StubSkillGapCategoryConfigService : ISkillGapCategoryConfigService
{
    public int GenerateCallCount { get; private set; }

    public Task GenerateCategoryConfigurationAsync(Guid roadmapId)
    {
        GenerateCallCount++;
        return Task.CompletedTask;
    }

    public Task<CategoryConfigurationResponseDto> GetCategoryConfigurationAsync(Guid actorUserId, Guid roadmapId)
    {
        return Task.FromResult(new CategoryConfigurationResponseDto());
    }

    public Task UpdateCategoryDisplayOrderAsync(
        Guid actorUserId,
        Guid roadmapId,
        List<UpdateCategoryDisplayOrderDto> request)
    {
        return Task.CompletedTask;
    }

    public Task<List<PublishedRoadmapOptionDto>> GetMyPublishedRoadmapsAsync(Guid userId)
    {
        return Task.FromResult(new List<PublishedRoadmapOptionDto>());
    }
}
