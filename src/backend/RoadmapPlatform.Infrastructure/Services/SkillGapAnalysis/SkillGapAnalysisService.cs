using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.SkillGapAnalysis;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.CareerRoleSkill;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using System.Text.Json;

namespace RoadmapPlatform.Infrastructure.Services.CareerRoleSkill
{
    public class SkillGapAnalysisService : ISkillGapAnalysisService
    {
        private readonly ApplicationDbContext _dbContext;

        public SkillGapAnalysisService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<AssessmentResponseDto> GetAssessmentAsync(string careerRoleSlug)
        {

            var careerRole = await _dbContext.CareerRoles
                .FirstOrDefaultAsync(x =>
                    x.Slug == careerRoleSlug);

            if (careerRole == null)
            {
                throw new NotFoundException("Career role not found.");
            }

            var roadmap = await _dbContext.Roadmaps
                .FirstOrDefaultAsync(x =>
                    x.CareerRoleId ==
                    careerRole.CareerRoleId);

            if (roadmap == null)
            {
                throw new NotFoundException("Roadmap not found.");
            }

            var roadmapVersion = await _dbContext.RoadmapVersions
                .Where(x =>
                    x.RoadmapId == roadmap.RoadmapId &&
                    x.Status == "published")
                .OrderByDescending(x =>
                    x.VersionNumber)
                .FirstOrDefaultAsync();

            if (roadmapVersion == null)
            {
                throw new NotFoundException("Published roadmap version not found.");
            }

            var phases = await _dbContext.RoadmapNodes
                .Where(x =>
                    x.RoadmapVersionId ==
                        roadmapVersion.RoadmapVersionId
                    &&
                    x.NodeType == "phase")
                .ToListAsync();

            var phaseMap = phases.ToDictionary(
                x => x.RoadmapNodeId);


            var groups = await _dbContext.RoadmapNodes
                .Where(x =>
                    x.RoadmapVersionId ==
                        roadmapVersion.RoadmapVersionId &&
                    x.NodeType == "choice_group" &&
                    x.IsAssessmentSkill)
                .ToListAsync();

            var edges = await _dbContext.RoadmapEdges
                .Where(x =>
                    x.RoadmapVersionId ==
                        roadmapVersion.RoadmapVersionId &&
                    x.EdgeType == "choice")
                .ToListAsync();

            var topics = await _dbContext.RoadmapNodes
                .Where(x =>
                    x.RoadmapVersionId ==
                        roadmapVersion.RoadmapVersionId &&
                    x.NodeType == "topic" &&
                    x.IsAssessmentSkill)
                .ToListAsync();

            var resultGroups = groups
                .Select(group =>
                {
                    var phase = phaseMap[group.ParentNodeId!.Value];

                    var topicIds = edges
                        .Where(e =>
                            e.FromNodeId == group.RoadmapNodeId)
                        .Select(e => e.ToNodeId)
                        .ToHashSet();

                    var skills = topics
                        .Where(t =>
                            topicIds.Contains(
                                t.RoadmapNodeId))
                        .Select(t =>
                            new AssessmentSkillDto
                            {
                                NodeId = t.RoadmapNodeId,
                                Name = t.Title,
                                Slug = t.Slug
                            })
                        .ToList();

                    return new AssessmentGroupDto
                    {
                        GroupId = group.RoadmapNodeId,
                        GroupName = group.Title,
                        GroupSlug = group.Slug,
                        PhaseName = phase.Title,
                        SortOrder = (phase.OrderIndex * 1000) + group.OrderIndex,
                        SelectionType = group.SelectionType,
                        RequiredCount = group.RequiredCount,
                        Skills = skills
                    };
                })
                .Where(x => x.Skills.Any())
                .OrderBy(x => x.SortOrder)
                .ToList();

            return new AssessmentResponseDto
            {
                CareerRoleName = careerRole?.Name ?? "",
                Groups = resultGroups
            };

        }

        public async Task<AnalyzeSkillGapResponseDto> AnalyzeAsync(Guid userId, AnalyzeSkillGapRequestDto request)
        {
            var careerRole = await _dbContext.CareerRoles
                .FirstOrDefaultAsync(x =>
                    x.Slug == request.CareerRoleSlug);

            if (careerRole == null)
            {
                throw new NotFoundException("Career role not found.");
            }


            var latestHistory =
                await _dbContext
                    .SkillGapAnalysisHistories
                    .Where(x =>
                        x.UserId == userId
                        &&
                        x.CareerRoleId ==
                            careerRole.CareerRoleId)
                    .OrderByDescending(x =>
                        x.CreatedAt)
                    .FirstOrDefaultAsync();

            if (latestHistory != null)
            {
                var nextAnalyzeAt =
                    latestHistory.CreatedAt
                        .AddDays(7);

                if (nextAnalyzeAt >
                    DateTime.UtcNow)
                {
                    throw new Exception(
                        $"You can analyze this role again after {nextAnalyzeAt:yyyy-MM-dd}");
                }
            }

            var roadmap = await _dbContext.Roadmaps
                .FirstOrDefaultAsync(x =>
                    x.CareerRoleId == careerRole.CareerRoleId);

            if (roadmap == null)
            {
                throw new NotFoundException("Roadmap not found.");
            }

            var roadmapVersion = await _dbContext.RoadmapVersions
                .Where(x =>
                    x.RoadmapId == roadmap.RoadmapId &&
                    x.Status == "published")
                .OrderByDescending(x =>
                    x.VersionNumber)
                .FirstOrDefaultAsync();

            if (roadmapVersion == null)
            {
                throw new NotFoundException(
                    "Published roadmap version not found.");
            }

            var phases = await _dbContext.RoadmapNodes
                .Where(x =>
                    x.RoadmapVersionId ==
                        roadmapVersion.RoadmapVersionId
                    &&
                    x.NodeType == "phase")
                .ToListAsync();

            var phaseMap =
                phases.ToDictionary(
                    x => x.RoadmapNodeId);


            var groups = await _dbContext.RoadmapNodes
                .Where(x =>
                    x.RoadmapVersionId ==
                        roadmapVersion.RoadmapVersionId &&
                    x.NodeType == "choice_group" &&
                    x.IsAssessmentSkill)
                .ToListAsync();

            var topics = await _dbContext.RoadmapNodes
                .Where(x =>
                    x.RoadmapVersionId ==
                        roadmapVersion.RoadmapVersionId &&
                    x.NodeType == "topic" &&
                    x.IsAssessmentSkill)
                .ToListAsync();

            var edges = await _dbContext.RoadmapEdges
                .Where(x =>
                    x.RoadmapVersionId ==
                        roadmapVersion.RoadmapVersionId &&
                    x.EdgeType == "choice")
                .ToListAsync();

            var selectedNodeIds =
                request.SelectedNodeIds.ToHashSet() ?? [];

            var totalSkills = topics.Count;

            var matchedSkills = topics.Count(x =>
                selectedNodeIds.Contains(
                    x.RoadmapNodeId));

            var skillCoveragePercent =
                totalSkills == 0
                    ? 0
                    : Math.Round(
                        matchedSkills * 100m / totalSkills,
                        2);


            var groupResults = groups
                .Select(group =>
                {

                    var phase =
                        phaseMap[
                            group.ParentNodeId!.Value
                        ];

                    var topicIds = edges
                        .Where(x =>
                            x.FromNodeId ==
                            group.RoadmapNodeId)
                        .Select(x =>
                            x.ToNodeId)
                        .ToHashSet();

                    var groupTopics = topics
                        .Where(x =>
                            topicIds.Contains(
                                x.RoadmapNodeId))
                        .ToList();

                    var groupMatched =
                        groupTopics.Count(x =>
                            selectedNodeIds.Contains(
                                x.RoadmapNodeId));

                    var groupTotal =
                        groupTopics.Count;

                    var groupMissingSkills = groupTopics
                        .Where(x =>
                            !selectedNodeIds.Contains(
                                x.RoadmapNodeId))
                        .Select(x =>
                            new MissingSkillDto
                            {
                                NodeId = x.RoadmapNodeId,
                                Name = x.Title
                            })
                        .ToList();


                    bool isCompleted = false;

                    if (group.SelectionType == "complete_all")
                    {
                        isCompleted =
                            groupMatched == groupTotal;
                    }
                    else if (group.SelectionType == "choose_many")
                    {
                        isCompleted =
                            groupMatched >=
                            (group.RequiredCount ?? 1);
                    }
                    else if (group.SelectionType == "choose_one")
                    {
                        isCompleted = groupMatched >= 1;
                    }
                    else
                    {
                        isCompleted = false;
                    }


                    return new SkillGapGroupResultDto
                    {
                        GroupName = group.Title,
                        TotalSkills = groupTotal,
                        MatchedSkills = groupMatched,
                        CompletionPercent =
                            groupTotal == 0
                                ? 0
                                : Math.Round(
                                    groupMatched * 100m /
                                    groupTotal,
                                    2),
                        SelectionType = group.SelectionType,
                        PhaseName = phase.Title,
                        SortOrder = (phase.OrderIndex * 1000) + group.OrderIndex,
                        RequiredCount = group.RequiredCount,
                        IsCompleted = isCompleted,
                        MissingSkills = groupMissingSkills

                    };
                })
                .Where(x => x.TotalSkills > 0)
                .OrderBy(x => x.SortOrder)
                .ToList();

            var totalGroups = groupResults.Count;
            var completedGroups = groupResults.Count(x => x.IsCompleted);
            var missingGroups = groupResults.Count(x => !x.IsCompleted);
            var readinessPercent =
                totalGroups == 0
                    ? 0
                    : Math.Round(
                        completedGroups * 100m /
                        totalGroups,
                        2);

            var response = new AnalyzeSkillGapResponseDto
            {
                CareerRoleName = careerRole.Name,
                TotalGroups = totalGroups,
                CompletedGroups = completedGroups,
                MissingGroups = missingGroups,
                ReadinessPercent = readinessPercent,
                TotalSkills = totalSkills,
                MatchedSkills = matchedSkills,
                SkillCoveragePercent = skillCoveragePercent,
                Groups = groupResults,
            };

            var snapshotJson = JsonSerializer.Serialize(response);

            var history =
    new SkillGapAnalysisHistory
    {
        SkillGapAnalysisHistoryId =
            Guid.NewGuid(),

        UserId =
            userId,

        CareerRoleId =
            careerRole.CareerRoleId,

        CareerRoleSlug =
            careerRole.Slug,

        CareerRoleName =
            careerRole.Name,

        ReadinessPercent =
            response.ReadinessPercent,

        SkillCoveragePercent =
            response.SkillCoveragePercent,

        SnapshotJson =
            snapshotJson,

        CreatedAt =
            DateTime.UtcNow
    };

            _dbContext
                .SkillGapAnalysisHistories
                .Add(history);

            await _dbContext
                .SaveChangesAsync();

            return response;
        }

        public async Task<List<CareerRoleOptionDto>> GetCareerRolesAsync()
        {
            return await _dbContext.CareerRoles
                .OrderBy(x => x.Name)
                .Select(x =>
                    new CareerRoleOptionDto
                    {
                        CareerRoleId = x.CareerRoleId,
                        Name = x.Name,
                        Slug = x.Slug
                    })
                .ToListAsync();
        }

        public async Task<List<AssessmentGroupAdminDto>> GetAssessmentGroupsAsync(string careerRoleSlug)
        {
            var careerRole = await _dbContext.CareerRoles
                .FirstOrDefaultAsync(x =>
                    x.Slug == careerRoleSlug);

            if (careerRole == null)
            {
                throw new NotFoundException(
                    "Career role not found.");
            }

            var roadmap = await _dbContext.Roadmaps
                .FirstOrDefaultAsync(x =>
                    x.CareerRoleId ==
                    careerRole.CareerRoleId);

            if (roadmap == null)
            {
                throw new NotFoundException(
                    "Roadmap not found.");
            }

            var roadmapVersion = await _dbContext.RoadmapVersions
                .Where(x =>
                    x.RoadmapId ==
                    roadmap.RoadmapId &&
                    x.Status == "published")
                .OrderByDescending(x =>
                    x.VersionNumber)
                .FirstOrDefaultAsync();

            if (roadmapVersion == null)
            {
                throw new NotFoundException(
                    "Published roadmap version not found.");
            }


            var phases = await _dbContext.RoadmapNodes
                .Where(x =>
                    x.RoadmapVersionId ==
                        roadmapVersion.RoadmapVersionId
                    &&
                    x.NodeType == "phase")
                .ToListAsync();

            var phaseMap = phases
                .ToDictionary(
                    x => x.RoadmapNodeId);


            var groups = await _dbContext.RoadmapNodes
                .Where(x =>
                    x.RoadmapVersionId ==
                    roadmapVersion.RoadmapVersionId &&
                    x.NodeType == "choice_group")
                .ToListAsync();

            var groupSortMap = groups
                .ToDictionary(
                    g => g.RoadmapNodeId,
                    g =>
                    {
                        var phase =
                            phaseMap[
                                g.ParentNodeId!.Value
                            ];

                        return
                            (
                                phase.OrderIndex * 1000
                            )
                            +
                            g.OrderIndex;
                    });



            var edges = await _dbContext.RoadmapEdges
                .Where(x =>
                    x.RoadmapVersionId ==
                    roadmapVersion.RoadmapVersionId &&
                    x.EdgeType == "choice")
                .ToListAsync();

            var result = groups
                .Select(group =>
                {
                    var topicIds = edges
                        .Where(x =>
                            x.FromNodeId ==
                            group.RoadmapNodeId)
                        .Select(x =>
                            x.ToNodeId)
                        .ToHashSet();

                    var phase = phaseMap[group.ParentNodeId!.Value];

                    var sortOrder = groupSortMap[group.RoadmapNodeId];

                    var totalTopics = topicIds.Count;

                    return new
                    {
                        SortOrder = sortOrder,
                        Data =
                            new AssessmentGroupAdminDto
                            {
                                GroupId =
                                    group.RoadmapNodeId,

                                GroupName =
                                    group.Title,

                                PhaseName = phase.Title,

                                SortOrder = sortOrder,

                                IsAssessmentSkill =
                                    group.IsAssessmentSkill,

                                TotalTopics = totalTopics,
                            }
                    };
                })
                .OrderBy(x =>
                    x.SortOrder)
                .Select(x =>
                    x.Data)
                .ToList();

            return result;
        }

        public async Task UpdateAssessmentGroupsAsync(List<UpdateAssessmentGroupDto> request)
        {
            var requestMap = request
                .ToDictionary(
                    x => x.GroupId,
                    x => x.IsAssessmentSkill);

            var nodeIds = requestMap.Keys.ToList();

            var groups = await _dbContext.RoadmapNodes
                .Where(x =>
                    nodeIds.Contains(
                        x.RoadmapNodeId)
                    &&
                    x.NodeType ==
                        "choice_group")
                .ToListAsync();

            if (groups.Count != request.Count)
            {
                throw new Exception(
                    "Invalid choice group.");
            }

            foreach (var group in groups)
            {
                group.IsAssessmentSkill =
                    requestMap[
                        group.RoadmapNodeId];
            }

            var groupEdges = await _dbContext.RoadmapEdges
                .Where(x =>
                    nodeIds.Contains(
                        x.FromNodeId)
                    &&
                    x.EdgeType == "choice")
                .ToListAsync();

            var childTopicIds =
                groupEdges
                    .Select(x =>
                        x.ToNodeId)
                    .Distinct()
                    .ToList();

            var childTopics =
                await _dbContext.RoadmapNodes
                    .Where(x =>
                        childTopicIds.Contains(
                            x.RoadmapNodeId))
                    .ToListAsync();

            foreach (var group in groups)
            {
                var enabled =
                    group.IsAssessmentSkill;

                var topicIds =
                    groupEdges
                        .Where(x =>
                            x.FromNodeId ==
                            group.RoadmapNodeId)
                        .Select(x =>
                            x.ToNodeId)
                        .ToHashSet();

                foreach (var topic in childTopics)
                {
                    if (topicIds.Contains(
                            topic.RoadmapNodeId))
                    {
                        topic.IsAssessmentSkill =
                            enabled;
                    }
                }
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<SkillGapHistoryDto>> GetHistoryAsync(Guid userId)
        {
            return await _dbContext
                .SkillGapAnalysisHistories
                .Where(x =>
                    x.UserId == userId)
                .OrderByDescending(x =>
                    x.CreatedAt)
                .Select(x =>
                    new SkillGapHistoryDto
                    {
                        HistoryId =
                            x.SkillGapAnalysisHistoryId,

                        CareerRoleName =
                            x.CareerRoleName,

                        ReadinessPercent =
                            x.ReadinessPercent,

                        SkillCoveragePercent =
                            x.SkillCoveragePercent,

                        CreatedAt =
                            x.CreatedAt
                    })
                .ToListAsync();
        }

        public async Task<SkillGapHistoryDetailDto> GetHistoryDetailAsync(Guid historyId, Guid userId)
        {
            var history =
                await _dbContext
                    .SkillGapAnalysisHistories
                    .FirstOrDefaultAsync(x =>
                        x.SkillGapAnalysisHistoryId
                            == historyId
                        &&
                        x.UserId
                            == userId);

            if (history == null)
            {
                throw new NotFoundException(
                    "History not found.");
            }

            var result =
                JsonSerializer.Deserialize<
                    AnalyzeSkillGapResponseDto>(
                        history.SnapshotJson);

            if (result == null)
            {
                throw new Exception(
                    "Invalid snapshot.");
            }

            return new SkillGapHistoryDetailDto
            {
                HistoryId =
                    history.SkillGapAnalysisHistoryId,

                CreatedAt =
                    history.CreatedAt,

                Result =
                    result
            };
        }

        public async Task DeleteHistoryAsync(Guid historyId, Guid userId)
        {
            var history =
                await _dbContext
                    .SkillGapAnalysisHistories
                    .FirstOrDefaultAsync(x =>
                        x.SkillGapAnalysisHistoryId
                            == historyId
                        &&
                        x.UserId
                            == userId);

            if (history == null)
            {
                throw new NotFoundException(
                    "History not found.");
            }

            _dbContext
                .SkillGapAnalysisHistories
                .Remove(history);

            await _dbContext
                .SaveChangesAsync();
        }


    }
}
