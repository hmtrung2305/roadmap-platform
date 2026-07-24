using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.SkillGapAnalysis.Assessment;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.SkillGapAnalysis;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services.Roadmaps;

namespace RoadmapPlatform.Infrastructure.Services.SkillGapAnalysis
{
    public class SkillGapAssessmentService : ISkillGapAssessmentService
    {
        private const string PublicRoadmapVisibility = "public";
        private const string PublishedVersionStatus = "published";
        private const string ActiveEnrollmentStatus = "active";
        private const string CompletedEnrollmentStatus = "completed";
        private const string CompletedNodeStatus = "completed";

        private readonly ApplicationDbContext _dbContext;

        public SkillGapAssessmentService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<AssessmentResponseDto> GetAssessmentAsync(
            Guid userId,
            Guid roadmapId,
            CancellationToken cancellationToken)
        {
            var roadmap = await GetPublishedRoadmapAsync(
                roadmapId,
                cancellationToken);

            var skills = await LoadPublishedAssessmentSkillsAsync(
                roadmap.PublishedVersion.RoadmapVersionId,
                cancellationToken);

            var categoryConfigs = await LoadCategoryConfigurationsAsync(
                roadmap.PublishedVersion.RoadmapVersionId,
                cancellationToken);

            if (categoryConfigs.Count == 0)
            {
                throw new ConflictException(
                    "Category configuration has not been generated.");
            }

            var completedNodeCountBySkillId =
                await GetCompletedSkillMetadataAsync(
                    userId,
                    roadmapId,
                    cancellationToken);

            var categories = BuildAssessmentCategories(
                categoryConfigs,
                skills,
                completedNodeCountBySkillId);

            return BuildAssessmentResponse(
                roadmap,
                categories);
        }

        private async Task<PublishedRoadmapSnapshot>
            GetPublishedRoadmapAsync(
                Guid roadmapId,
                CancellationToken cancellationToken)
        {
            var roadmap = await _dbContext.Roadmaps
                .AsNoTracking()
                .Where(x =>
                    x.RoadmapId == roadmapId &&
                    x.Visibility == PublicRoadmapVisibility)
                .Select(x => new
                {
                    x.RoadmapId,

                    RoadmapName = x.Title,

                    CareerRoleName = x.CareerRole.Name,

                    AuthorName =
                        !string.IsNullOrWhiteSpace(
                            x.OwnerUser!.UserProfile!.DisplayName)
                            ? x.OwnerUser.UserProfile.DisplayName!
                            : x.OwnerUser.Username,

                    PublishedVersion = x.RoadmapVersions
                        .Where(v =>
                            v.Status == PublishedVersionStatus)
                        .Select(v => new
                        {
                            v.RoadmapVersionId,
                            v.Title,
                            v.VersionNumber,
                        })
                        .SingleOrDefault(),
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (roadmap is null)
            {
                throw new NotFoundException("Roadmap not found.");
            }

            if (roadmap.PublishedVersion is null)
            {
                throw new ConflictException(
                    "Roadmap does not have a published version.");
            }

            var publishedVersion =
                new PublishedRoadmapVersionSnapshot(
                    roadmap.PublishedVersion.RoadmapVersionId,
                    roadmap.PublishedVersion.Title,
                    roadmap.PublishedVersion.VersionNumber);

            return new PublishedRoadmapSnapshot(
                roadmap.RoadmapId,
                roadmap.RoadmapName,
                roadmap.CareerRoleName,
                roadmap.AuthorName,
                publishedVersion);
        }

        private async Task<List<PublishedAssessmentSkillSnapshot>>
            LoadPublishedAssessmentSkillsAsync(
                Guid roadmapVersionId,
                CancellationToken cancellationToken)
        {
            var skillRows = await _dbContext.RoadmapNodeSkills
                .AsNoTracking()
                .Where(x =>
                    x.RoadmapNode.RoadmapVersionId ==
                    roadmapVersionId)
                .Select(x => new
                {
                    x.Skill.SkillId,

                    SkillName = x.Skill.Name,

                    CategoryName = x.Skill.Category,
                })
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.CategoryName))
                .Distinct()
                .ToListAsync(cancellationToken);

            return skillRows
                .Select(x =>
                    new PublishedAssessmentSkillSnapshot(
                        x.SkillId,
                        x.SkillName,
                        x.CategoryName!))
                .ToList();
        }

        private async Task<List<CategoryConfigurationSnapshot>>
            LoadCategoryConfigurationsAsync(
                Guid roadmapVersionId,
                CancellationToken cancellationToken)
        {
            var categoryRows =
                await _dbContext.SkillGapCategoryConfigs
                    .AsNoTracking()
                    .Where(x =>
                        x.RoadmapVersionId == roadmapVersionId)
                    .Select(x => new
                    {
                        x.CategoryName,
                        x.DisplayOrder,
                    })
                    .ToListAsync(cancellationToken);

            return categoryRows
                .Select(x =>
                    new CategoryConfigurationSnapshot(
                        x.CategoryName,
                        x.DisplayOrder))
                .ToList();
        }

        private async Task<Dictionary<Guid, int>>
            GetCompletedSkillMetadataAsync(
                Guid userId,
                Guid roadmapId,
                CancellationToken cancellationToken)
        {
            var enrollment = await GetRelevantEnrollmentAsync(
                userId,
                roadmapId,
                cancellationToken);

            if (enrollment is null)
            {
                return new Dictionary<Guid, int>();
            }

            var progressSnapshot = await LoadProgressSnapshotAsync(
                enrollment.EnrollmentId,
                enrollment.RoadmapVersionId,
                cancellationToken);

            var completedNodeIds =
                GetCompletedNodeIds(progressSnapshot);

            if (completedNodeIds.Count == 0)
            {
                return new Dictionary<Guid, int>();
            }

            return await GetCompletedNodeCountBySkillIdAsync(
                completedNodeIds,
                cancellationToken);
        }

        private async Task<RoadmapEnrollmentSnapshot?>
            GetRelevantEnrollmentAsync(
                Guid userId,
                Guid roadmapId,
                CancellationToken cancellationToken)
        {
            var enrollment = await _dbContext.RoadmapEnrollments
                .AsNoTracking()
                .Where(x =>
                    x.UserId == userId &&
                    x.RoadmapVersion.RoadmapId == roadmapId &&
                    (x.Status == ActiveEnrollmentStatus ||
                     x.Status == CompletedEnrollmentStatus))
                .OrderBy(x =>
                    x.Status == ActiveEnrollmentStatus ? 0 : 1)
                .ThenByDescending(x => x.UpdatedAt)
                .ThenByDescending(x => x.StartedAt)
                .Select(x => new
                {
                    EnrollmentId = x.RoadmapEnrollmentId,
                    x.RoadmapVersionId,
                    x.Status,
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (enrollment is null)
            {
                return null;
            }

            return new RoadmapEnrollmentSnapshot(
                enrollment.EnrollmentId,
                enrollment.RoadmapVersionId,
                enrollment.Status);
        }

        private async Task<RoadmapProgressSnapshot>
            LoadProgressSnapshotAsync(
                Guid enrollmentId,
                Guid roadmapVersionId,
                CancellationToken cancellationToken)
        {
            var nodeRows = await _dbContext.RoadmapNodes
                .AsNoTracking()
                .Where(x =>
                    x.RoadmapVersionId == roadmapVersionId)
                .Select(x => new
                {
                    x.RoadmapNodeId,
                    x.ParentNodeId,
                    x.NodeType,
                    x.SelectionType,
                    x.RequiredCount,
                    x.OrderIndex,
                    x.IsRequired,
                })
                .ToListAsync(cancellationToken);

            var nodes = nodeRows
                .Select(x => new RoadmapNode
                {
                    RoadmapNodeId = x.RoadmapNodeId,

                    RoadmapVersionId = roadmapVersionId,

                    ParentNodeId = x.ParentNodeId,

                    NodeType = x.NodeType,

                    SelectionType = x.SelectionType,

                    RequiredCount = x.RequiredCount,

                    OrderIndex = x.OrderIndex,

                    IsRequired = x.IsRequired,
                })
                .ToList();

            var edgeRows = await _dbContext.RoadmapEdges
                .AsNoTracking()
                .Where(x =>
                    x.RoadmapVersionId == roadmapVersionId)
                .Select(x => new
                {
                    x.FromNodeId,
                    x.ToNodeId,
                    x.EdgeType,
                    x.DependencyType,
                })
                .ToListAsync(cancellationToken);

            var edges = edgeRows
                .Select(x => new RoadmapEdge
                {
                    RoadmapVersionId = roadmapVersionId,

                    FromNodeId = x.FromNodeId,

                    ToNodeId = x.ToNodeId,

                    EdgeType = x.EdgeType,

                    DependencyType = x.DependencyType,
                })
                .ToList();

            var progressRows = await _dbContext.UserNodeProgresses
                .AsNoTracking()
                .Where(x =>
                    x.RoadmapEnrollmentId == enrollmentId &&
                    x.RoadmapNode.RoadmapVersionId ==
                    roadmapVersionId)
                .Select(x => new
                {
                    x.RoadmapNodeId,
                    x.Status,
                })
                .ToListAsync(cancellationToken);

            var progressByNodeId = progressRows
                .ToDictionary(
                    x => x.RoadmapNodeId,
                    x => new UserNodeProgress
                    {
                        RoadmapEnrollmentId = enrollmentId,

                        RoadmapNodeId = x.RoadmapNodeId,

                        Status = x.Status,
                    });

            return new RoadmapProgressSnapshot(
                nodes,
                edges,
                progressByNodeId);
        }

        private async Task<Dictionary<Guid, int>>
            GetCompletedNodeCountBySkillIdAsync(
                IReadOnlySet<Guid> completedNodeIds,
                CancellationToken cancellationToken)
        {
            if (completedNodeIds.Count == 0)
            {
                return new Dictionary<Guid, int>();
            }

            var completedNodeIdArray =
                completedNodeIds.ToArray();

            var skillNodePairs = await _dbContext.RoadmapNodeSkills
                .AsNoTracking()
                .Where(x =>
                    completedNodeIdArray.Contains(
                        x.RoadmapNodeId))
                .Select(x => new
                {
                    x.SkillId,
                    x.RoadmapNodeId,
                })
                .Distinct()
                .ToListAsync(cancellationToken);

            return skillNodePairs
                .GroupBy(x => x.SkillId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(x => x.RoadmapNodeId)
                        .Distinct()
                        .Count());
        }

        private static HashSet<Guid> GetCompletedNodeIds(
            RoadmapProgressSnapshot snapshot)
        {
            var effectiveStatuses =
                RoadmapProgressCalculator.CalculateStatuses(
                    snapshot.Nodes,
                    snapshot.Edges,
                    snapshot.ProgressByNodeId);

            return effectiveStatuses
                .Where(x =>
                    x.Value == CompletedNodeStatus)
                .Select(x => x.Key)
                .ToHashSet();
        }

        private static List<AssessmentCategoryDto>
            BuildAssessmentCategories(
                IReadOnlyList<CategoryConfigurationSnapshot>
                    categoryConfigs,
                IReadOnlyList<PublishedAssessmentSkillSnapshot>
                    skills,
                IReadOnlyDictionary<Guid, int>
                    completedNodeCountBySkillId)
        {
            return categoryConfigs
                .GroupJoin(
                    skills,
                    config => config.CategoryName,
                    skill => skill.CategoryName,
                    (config, categorySkills) =>
                        new AssessmentCategoryDto
                        {
                            CategoryName = config.CategoryName,

                            DisplayOrder = config.DisplayOrder,

                            Skills = categorySkills
                                .OrderBy(x => x.SkillName)
                                .Select(x =>
                                    BuildAssessmentSkill(
                                        x,
                                        completedNodeCountBySkillId))
                                .ToList(),
                        })
                .OrderBy(x => x.DisplayOrder)
                .ToList();
        }

        private static AssessmentSkillDto BuildAssessmentSkill(
            PublishedAssessmentSkillSnapshot skill,
            IReadOnlyDictionary<Guid, int>
                completedNodeCountBySkillId)
        {
            var completedNodeCount =
                completedNodeCountBySkillId.GetValueOrDefault(
                    skill.SkillId);

            return new AssessmentSkillDto
            {
                SkillId = skill.SkillId,

                SkillName = skill.SkillName,

                IsSuggestedFromCompletedNodes =
                    completedNodeCount > 0,

                CompletedNodeCount = completedNodeCount,
            };
        }

        private static AssessmentResponseDto BuildAssessmentResponse(
            PublishedRoadmapSnapshot roadmap,
            List<AssessmentCategoryDto> categories)
        {
            return new AssessmentResponseDto
            {
                RoadmapId = roadmap.RoadmapId,

                RoadmapName = roadmap.RoadmapName,

                CareerRoleName = roadmap.CareerRoleName,

                RoadmapVersionTitle =
                    roadmap.PublishedVersion.Title,

                RoadmapVersionNumber =
                    roadmap.PublishedVersion.VersionNumber,

                AuthorName = roadmap.AuthorName,

                Categories = categories,
            };
        }

        private sealed record PublishedRoadmapSnapshot(
            Guid RoadmapId,
            string RoadmapName,
            string CareerRoleName,
            string AuthorName,
            PublishedRoadmapVersionSnapshot PublishedVersion);

        private sealed record PublishedRoadmapVersionSnapshot(
            Guid RoadmapVersionId,
            string Title,
            int VersionNumber);

        private sealed record PublishedAssessmentSkillSnapshot(
            Guid SkillId,
            string SkillName,
            string CategoryName);

        private sealed record CategoryConfigurationSnapshot(
            string CategoryName,
            int DisplayOrder);

        private sealed record RoadmapEnrollmentSnapshot(
            Guid EnrollmentId,
            Guid RoadmapVersionId,
            string Status);

        private sealed record RoadmapProgressSnapshot(
            IReadOnlyList<RoadmapNode> Nodes,
            IReadOnlyList<RoadmapEdge> Edges,
            IReadOnlyDictionary<Guid, UserNodeProgress>
                ProgressByNodeId);
    }
}