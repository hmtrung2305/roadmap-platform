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

        public async Task<List<CareerRoleOptionDto>> GetCareerRolesAsync()
        {
            return await _dbContext.CareerRoles
                .OrderBy(x => x.Name)
                .Select(x => new CareerRoleOptionDto
                {
                    CareerRoleId = x.CareerRoleId,
                    Name = x.Name,
                    Slug = x.Slug
                })
                .ToListAsync();
        }

        // USER
        public async Task<List<AssessmentLevelDto>> GetAssessmentLevelsAsync(string careerRoleSlug)
        {
            var careerRole = await _dbContext.CareerRoles
                .FirstOrDefaultAsync(x => x.Slug == careerRoleSlug);

            if (careerRole == null)
            {
                throw new NotFoundException("Career role not found.");
            }

            return await _dbContext.AssessmentLevels
                .Where(x => x.CareerRoleId == careerRole.CareerRoleId)
                .OrderBy(x => x.SortOrder)
                .Select(x => new AssessmentLevelDto
                {
                    LevelId = x.AssessmentLevelId,
                    LevelName = x.Name,
                    Slug = x.Slug
                })
                .ToListAsync();
        }

        public async Task<AssessmentByLevelResponseDto> GetAssessmentByLevelAsync(
            string careerRoleSlug,
            string levelSlug)
        {
            var careerRole = await _dbContext.CareerRoles
                .FirstOrDefaultAsync(x => x.Slug == careerRoleSlug);

            if (careerRole == null)
            {
                throw new NotFoundException("Career role not found.");
            }

            var level = await _dbContext.AssessmentLevels
                .FirstOrDefaultAsync(x =>
                    x.CareerRoleId == careerRole.CareerRoleId &&
                    x.Slug == levelSlug);

            if (level == null)
            {
                throw new NotFoundException("Assessment level not found.");
            }

            var levelGroupIds = await _dbContext.AssessmentLevelGroups
                .Where(x => x.AssessmentLevelId == level.AssessmentLevelId)
                .Select(x => x.RoadmapNodeId)
                .ToListAsync();

            var roadmap = await _dbContext.Roadmaps
                .FirstOrDefaultAsync(x => x.CareerRoleId == careerRole.CareerRoleId);

            if (roadmap == null)
            {
                throw new NotFoundException("Roadmap not found.");
            }

            var roadmapVersion = await _dbContext.RoadmapVersions
                .Where(x =>
                    x.RoadmapId == roadmap.RoadmapId &&
                    x.Status == "published")
                .OrderByDescending(x => x.VersionNumber)
                .FirstOrDefaultAsync();

            if (roadmapVersion == null)
            {
                throw new NotFoundException("Published roadmap version not found.");
            }

            var groups = await _dbContext.RoadmapNodes
                .Where(x =>
                    levelGroupIds.Contains(x.RoadmapNodeId) &&
                    x.NodeType == "choice_group" &&
                    x.RoadmapVersionId == roadmapVersion.RoadmapVersionId)
                .ToListAsync();

            var phases = await _dbContext.RoadmapNodes
                .Where(x =>
                    x.RoadmapVersionId == roadmapVersion.RoadmapVersionId &&
                    x.NodeType == "phase")
                .ToListAsync();

            var phaseMap = phases.ToDictionary(x => x.RoadmapNodeId);

            var edges = await _dbContext.RoadmapEdges
                .Where(x =>
                    x.RoadmapVersionId == roadmapVersion.RoadmapVersionId &&
                    x.EdgeType == "choice")
                .ToListAsync();

            var topics = await _dbContext.RoadmapNodes
                .Where(x =>
                    x.RoadmapVersionId == roadmapVersion.RoadmapVersionId &&
                    x.NodeType == "topic")
                .ToListAsync();

            var resultGroups = groups
                .Select(group =>
                {
                    if (!group.ParentNodeId.HasValue ||
                        !phaseMap.TryGetValue(group.ParentNodeId.Value, out var phase))
                    {
                        return null;
                    }

                    var topicIds = edges
                        .Where(x => x.FromNodeId == group.RoadmapNodeId)
                        .Select(x => x.ToNodeId)
                        .ToHashSet();

                    var skills = topics
                        .Where(x => topicIds.Contains(x.RoadmapNodeId))
                        .Select(x => new AssessmentSkillDto
                        {
                            NodeId = x.RoadmapNodeId,
                            Name = x.Title,
                            Slug = x.Slug
                        })
                        .ToList();

                    return new AssessmentGroupByLevelDto
                    {
                        GroupId = group.RoadmapNodeId,
                        GroupName = group.Title,
                        GroupSlug = group.Slug,
                        SelectionType = group.SelectionType,
                        PhaseName = phase.Title,
                        SortOrder = (phase.OrderIndex * 1000) + group.OrderIndex,
                        RequiredCount = group.RequiredCount,
                        Skills = skills
                    };
                })
                .Where(x => x != null)
                .Where(x => x!.Skills.Any())
                .OrderBy(x => x!.SortOrder)
                .Select(x => x!)
                .ToList();

            return new AssessmentByLevelResponseDto
            {
                LevelId = level.AssessmentLevelId,
                LevelName = level.Name,
                LevelSlug = level.Slug,
                CareerRoleName = careerRole.Name,
                RoadmapVersionNumber = roadmapVersion.VersionNumber,
                RoadmapVersionTitle = roadmapVersion.Title,
                Groups = resultGroups
            };
        }

        public async Task<AnalyzeSkillGapResponseDto> AnalyzeAsync(
            Guid userId,
            AnalyzeSkillGapRequestDto request)
        {
            var careerRole = await _dbContext.CareerRoles
                .FirstOrDefaultAsync(x => x.Slug == request.CareerRoleSlug);

            if (careerRole == null)
            {
                throw new NotFoundException("Career role not found.");
            }

            var level = await _dbContext.AssessmentLevels
                .FirstOrDefaultAsync(x =>
                    x.CareerRoleId == careerRole.CareerRoleId &&
                    x.Slug == request.LevelSlug);

            if (level == null)
            {
                throw new NotFoundException("Assessment level not found.");
            }

            var levelGroupIds = await _dbContext.AssessmentLevelGroups
                .Where(x => x.AssessmentLevelId == level.AssessmentLevelId)
                .Select(x => x.RoadmapNodeId)
                .ToListAsync();

            var latestHistory = await _dbContext.SkillGapAnalysisHistories
                .Where(x =>
                    x.UserId == userId &&
                    x.CareerRoleId == careerRole.CareerRoleId &&
                    x.LevelSlug == request.LevelSlug)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (latestHistory != null)
            {
                var nextAnalyzeAt = latestHistory.CreatedAt.AddDays(3);

                if (nextAnalyzeAt > DateTime.UtcNow)
                {

                    throw new ConflictException($"You can analyze again after {nextAnalyzeAt:yyyy-MM-dd HH:mm} UTC.");
                }
            }

            var roadmap = await _dbContext.Roadmaps
                .FirstOrDefaultAsync(x => x.CareerRoleId == careerRole.CareerRoleId);

            if (roadmap == null)
            {
                throw new NotFoundException("Roadmap not found.");
            }

            var roadmapVersion = await _dbContext.RoadmapVersions
                .Where(x =>
                    x.RoadmapId == roadmap.RoadmapId &&
                    x.Status == "published")
                .OrderByDescending(x => x.VersionNumber)
                .FirstOrDefaultAsync();

            if (roadmapVersion == null)
            {
                throw new NotFoundException("Published roadmap version not found.");
            }

            var phases = await _dbContext.RoadmapNodes
                .Where(x =>
                    x.RoadmapVersionId == roadmapVersion.RoadmapVersionId &&
                    x.NodeType == "phase")
                .ToListAsync();

            var phaseMap = phases.ToDictionary(x => x.RoadmapNodeId);

            var groups = await _dbContext.RoadmapNodes
                .Where(x =>
                    levelGroupIds.Contains(x.RoadmapNodeId) &&
                    x.NodeType == "choice_group" &&
                    x.RoadmapVersionId == roadmapVersion.RoadmapVersionId)
                .ToListAsync();

            var topics = await _dbContext.RoadmapNodes
                .Where(x =>
                    x.RoadmapVersionId == roadmapVersion.RoadmapVersionId &&
                    x.NodeType == "topic")
                .ToListAsync();

            var edges = await _dbContext.RoadmapEdges
                .Where(x =>
                    x.RoadmapVersionId == roadmapVersion.RoadmapVersionId &&
                    x.EdgeType == "choice")
                .ToListAsync();

            var assessmentTopicIds = edges
                .Where(x => levelGroupIds.Contains(x.FromNodeId))
                .Select(x => x.ToNodeId)
                .Distinct()
                .ToHashSet();

            var assessmentTopics = topics
                .Where(x => assessmentTopicIds.Contains(x.RoadmapNodeId))
                .ToList();

            var selectedNodeIds = request.SelectedNodeIds.ToHashSet() ?? [];

            var totalSkills = assessmentTopics.Count;

            var matchedSkills = assessmentTopics.Count(x =>
                selectedNodeIds.Contains(x.RoadmapNodeId));

            var missingSkills = totalSkills - matchedSkills;

            var groupResults = groups
                .Select(group =>
                {
                    if (!group.ParentNodeId.HasValue ||
                        !phaseMap.TryGetValue(group.ParentNodeId.Value, out var phase))
                    {
                        return null;
                    }

                    var topicIds = edges
                        .Where(x => x.FromNodeId == group.RoadmapNodeId)
                        .Select(x => x.ToNodeId)
                        .ToHashSet();

                    var groupTopics = assessmentTopics
                        .Where(x => topicIds.Contains(x.RoadmapNodeId))
                        .ToList();

                    var groupMatched = groupTopics.Count(x =>
                        selectedNodeIds.Contains(x.RoadmapNodeId));

                    var groupTotal = groupTopics.Count;

                    var groupMissingSkills = groupTopics
                        .Where(x => !selectedNodeIds.Contains(x.RoadmapNodeId))
                        .Select(x => new MissingSkillDto
                        {
                            NodeId = x.RoadmapNodeId,
                            Name = x.Title
                        })
                        .ToList();

                    bool isCompleted = false;

                    if (group.SelectionType == "complete_all")
                    {
                        isCompleted = groupMatched == groupTotal;
                    }
                    else if (group.SelectionType == "choose_many")
                    {
                        isCompleted = groupMatched >= (group.RequiredCount ?? 1);
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
                        SelectionType = group.SelectionType,
                        PhaseName = phase.Title,
                        SortOrder = (phase.OrderIndex * 1000) + group.OrderIndex,
                        RequiredCount = group.RequiredCount,
                        IsCompleted = isCompleted,
                        MissingSkills = groupMissingSkills
                    };
                })
                .Where(x => x != null)
                .Cast<SkillGapGroupResultDto>()
                .Where(x => x.TotalSkills > 0)
                .OrderBy(x => x.SortOrder)
                .ToList();

            var totalGroups = groupResults.Count;
            var completedGroups = groupResults.Count(x => x.IsCompleted);
            var missingGroups = groupResults.Count(x => !x.IsCompleted);

            var response = new AnalyzeSkillGapResponseDto
            {
                CareerRoleName = careerRole.Name,
                LevelName = level.Name,
                LevelSlug = level.Slug,
                RoadmapVersionNumber = roadmapVersion.VersionNumber,
                RoadmapVersionTitle = roadmapVersion.Title,
                TotalGroups = totalGroups,
                CompletedGroups = completedGroups,
                MissingGroups = missingGroups,
                TotalSkills = totalSkills,
                MatchedSkills = matchedSkills,
                MissingSkills = missingSkills,
                Groups = groupResults,
            };

            var snapshotJson = JsonSerializer.Serialize(response);

            var history = new SkillGapAnalysisHistory
            {
                SkillGapAnalysisHistoryId = Guid.NewGuid(),
                UserId = userId,
                CareerRoleId = careerRole.CareerRoleId,
                CareerRoleSlug = careerRole.Slug,
                CareerRoleName = careerRole.Name,
                LevelSlug = level.Slug,
                LevelName = level.Name,
                RoadmapVersionNumber = roadmapVersion.VersionNumber,
                RoadmapVersionTitle = roadmapVersion.Title,
                MatchedSkills = matchedSkills,
                TotalSkills = totalSkills,
                MissingSkills = missingSkills,
                SnapshotJson = snapshotJson,
                IsDeleted = false,
                DeletedAt = null,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.SkillGapAnalysisHistories.Add(history);

            await _dbContext.SaveChangesAsync();

            return response;
        }

        public async Task<List<SkillGapHistoryDto>> GetHistoryAsync(Guid userId)
        {
            return await _dbContext.SkillGapAnalysisHistories
                .Where(x => x.UserId == userId && !x.IsDeleted)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new SkillGapHistoryDto
                {
                    HistoryId = x.SkillGapAnalysisHistoryId,
                    CareerRoleName = x.CareerRoleName,
                    LevelName = x.LevelName,
                    RoadmapVersionNumber = x.RoadmapVersionNumber,
                    RoadmapVersionTitle = x.RoadmapVersionTitle,
                    MatchedSkills = x.MatchedSkills,
                    TotalSkills = x.TotalSkills,
                    MissingSkills = x.MissingSkills,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<SkillGapHistoryDetailDto> GetHistoryDetailAsync(
            Guid historyId,
            Guid userId)
        {
            var history = await _dbContext.SkillGapAnalysisHistories
                .FirstOrDefaultAsync(x =>
                    x.SkillGapAnalysisHistoryId == historyId &&
                    x.UserId == userId &&
                    !x.IsDeleted);

            if (history == null)
            {
                throw new NotFoundException("History not found.");
            }

            var result = JsonSerializer.Deserialize<AnalyzeSkillGapResponseDto>(
                history.SnapshotJson);

            if (result == null)
            {
                throw new Exception("Invalid snapshot.");
            }

            return new SkillGapHistoryDetailDto
            {
                HistoryId = history.SkillGapAnalysisHistoryId,
                CreatedAt = history.CreatedAt,
                Result = result
            };
        }

        public async Task DeleteHistoryAsync(Guid historyId, Guid userId)
        {
            var history = await _dbContext.SkillGapAnalysisHistories
                .FirstOrDefaultAsync(x =>
                    x.SkillGapAnalysisHistoryId == historyId &&
                    x.UserId == userId &&
                    !x.IsDeleted);

            if (history == null)
            {
                throw new NotFoundException("History not found.");
            }

            history.IsDeleted = true;
            history.DeletedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
        }

        // ADMIN
        public async Task UpdateAssessmentLevelGroupsAsync(
            string careerRoleSlug,
            string levelSlug,
            UpdateAssessmentLevelGroupsDto request)
        {
            request.GroupIds = request.GroupIds
                .Distinct()
                .ToList();

            var level = await _dbContext.AssessmentLevels
                .Include(x => x.CareerRole)
                .FirstOrDefaultAsync(x =>
                    x.CareerRole.Slug == careerRoleSlug &&
                    x.Slug == levelSlug);

            if (level == null)
            {
                throw new NotFoundException("Assessment level not found.");
            }

            var roadmap = await _dbContext.Roadmaps
                .FirstOrDefaultAsync(x => x.CareerRoleId == level.CareerRoleId);

            if (roadmap == null)
            {
                throw new NotFoundException("Roadmap not found.");
            }

            var roadmapVersion = await _dbContext.RoadmapVersions
                .Where(x =>
                    x.RoadmapId == roadmap.RoadmapId &&
                    x.Status == "published")
                .OrderByDescending(x => x.VersionNumber)
                .FirstOrDefaultAsync();

            if (roadmapVersion == null)
            {
                throw new NotFoundException("Published roadmap version not found.");
            }

            var groups = await _dbContext.RoadmapNodes
                .Where(x =>
                    request.GroupIds.Contains(x.RoadmapNodeId) &&
                    x.NodeType == "choice_group" &&
                    x.RoadmapVersionId == roadmapVersion.RoadmapVersionId)
                .ToListAsync();

            if (groups.Count != request.GroupIds.Count)
            {
                throw new Exception("Invalid choice group.");
            }

            var existingMappings = await _dbContext.AssessmentLevelGroups
                .Where(x => x.AssessmentLevelId == level.AssessmentLevelId)
                .ToListAsync();

            _dbContext.AssessmentLevelGroups.RemoveRange(existingMappings);

            var newMappings = request.GroupIds
                .Select(groupId => new AssessmentLevelGroup
                {
                    AssessmentLevelGroupId = Guid.NewGuid(),
                    AssessmentLevelId = level.AssessmentLevelId,
                    RoadmapNodeId = groupId
                })
                .ToList();

            await _dbContext.AssessmentLevelGroups.AddRangeAsync(newMappings);

            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<AssessmentLevelContentManagerDto>> GetAssessmentLevelsContentManagerAsync(
            string careerRoleSlug)
        {
            var careerRole = await _dbContext.CareerRoles
                .FirstOrDefaultAsync(x => x.Slug == careerRoleSlug);

            if (careerRole == null)
            {
                throw new NotFoundException("Career role not found.");
            }

            return await _dbContext.AssessmentLevels
                .Where(x => x.CareerRoleId == careerRole.CareerRoleId)
                .OrderBy(x => x.SortOrder)
                .Select(x => new AssessmentLevelContentManagerDto
                {
                    LevelId = x.AssessmentLevelId,
                    LevelName = x.Name,
                    Slug = x.Slug,
                    GroupCount = x.AssessmentLevelGroups.Count
                })
                .ToListAsync();
        }

        public async Task<AssessmentLevelGroupsContentManagerDto> GetAssessmentGroupsByLevelContentManagerAsync(
            string careerRoleSlug,
            string levelSlug)
        {
            var level = await _dbContext.AssessmentLevels
                .Include(x => x.CareerRole)
                .FirstOrDefaultAsync(x =>
                    x.CareerRole.Slug == careerRoleSlug &&
                    x.Slug == levelSlug);

            if (level == null)
            {
                throw new NotFoundException("Assessment level not found.");
            }

            var roadmap = await _dbContext.Roadmaps
                .FirstOrDefaultAsync(x => x.CareerRoleId == level.CareerRoleId);

            if (roadmap == null)
            {
                throw new NotFoundException("Roadmap not found.");
            }

            var roadmapVersion = await _dbContext.RoadmapVersions
                .Where(x =>
                    x.RoadmapId == roadmap.RoadmapId &&
                    x.Status == "published")
                .OrderByDescending(x => x.VersionNumber)
                .FirstOrDefaultAsync();

            if (roadmapVersion == null)
            {
                throw new NotFoundException("Published roadmap version not found.");
            }

            var phases = await _dbContext.RoadmapNodes
                .Where(x =>
                    x.RoadmapVersionId == roadmapVersion.RoadmapVersionId &&
                    x.NodeType == "phase")
                .ToListAsync();

            var phaseMap = phases.ToDictionary(x => x.RoadmapNodeId);

            var groups = await _dbContext.RoadmapNodes
                .Where(x =>
                    x.RoadmapVersionId == roadmapVersion.RoadmapVersionId &&
                    x.NodeType == "choice_group")
                .ToListAsync();

            var selectedGroupIds = await _dbContext.AssessmentLevelGroups
                .Where(x => x.AssessmentLevelId == level.AssessmentLevelId)
                .Select(x => x.RoadmapNodeId)
                .ToHashSetAsync();

            var resultGroups = groups
                .Select(group =>
                {
                    if (!group.ParentNodeId.HasValue ||
                        !phaseMap.TryGetValue(group.ParentNodeId.Value, out var phase))
                    {
                        return null;
                    }

                    return new AssessmentGroupContentManagerDto
                    {
                        GroupId = group.RoadmapNodeId,
                        GroupName = group.Title,
                        PhaseName = phase.Title,
                        GroupSlug = group.Slug,
                        SortOrder = (phase.OrderIndex * 1000) + group.OrderIndex,
                        Selected = selectedGroupIds.Contains(group.RoadmapNodeId)
                    };
                })
                .Where(x => x != null)
                .Cast<AssessmentGroupContentManagerDto>()
                .OrderBy(x => x.SortOrder)
                .ToList();

            return new AssessmentLevelGroupsContentManagerDto
            {
                CareerRoleName = level.CareerRole.Name,
                LevelName = level.Name,
                LevelSlug = level.Slug,
                GroupCount = resultGroups.Count,
                RoadmapVersionNumber = roadmapVersion.VersionNumber,
                RoadmapVersionTitle = roadmapVersion.Title,
                Groups = resultGroups
            };
        }
    }
}
