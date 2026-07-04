using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.SkillGapAnalysis.Analysis;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.SkillGapAnalysis;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using System.Text.Json;

namespace RoadmapPlatform.Infrastructure.Services.SkillGapAnalysis
{
    public class SkillGapAnalysisService : ISkillGapAnalysisService
    {
        private readonly ApplicationDbContext _dbContext;

        public SkillGapAnalysisService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<AnalyzeSkillGapResponseDto> AnalyzeAsync(Guid userId, AnalyzeSkillGapRequestDto request)
        {
            var lastAnalysisAt = await _dbContext.SkillGapAnalysisHistories
                .AsNoTracking()
                .Where(x =>
                    x.UserId == userId &&
                    x.RoadmapId == request.RoadmapId)
                .MaxAsync(x => (DateTime?)x.CreatedAt);

            if (lastAnalysisAt.HasValue)
            {
                var availableAt = lastAnalysisAt.Value.AddDays(3);

                if (availableAt > DateTime.UtcNow)
                {
                    throw new ConflictException(
                        $"You can analyze this roadmap again after {availableAt:yyyy-MM-dd HH:mm:ss} UTC.");
                }
            }

            var roadmap = await _dbContext.Roadmaps
                .AsNoTracking()
                .Where(x => x.RoadmapId == request.RoadmapId)
                .Select(x => new
                {
                    x.RoadmapId,

                    RoadmapName = x.Title,

                    CareerRoleId = x.CareerRoleId,

                    CareerRoleName = x.CareerRole.Name,

                    AuthorName =
                        !string.IsNullOrWhiteSpace(x.OwnerUser!.UserProfile!.DisplayName)
                            ? x.OwnerUser.UserProfile.DisplayName!
                            : x.OwnerUser.Username,

                    PublishedVersion = x.RoadmapVersions
                        .Where(v => v.Status == "published")
                        .Select(v => new
                        {
                            v.RoadmapVersionId,

                            v.Title,

                            v.VersionNumber,
                        })
                        .SingleOrDefault()
                })
                .SingleOrDefaultAsync();

            if (roadmap is null)
            {
                throw new NotFoundException("Roadmap not found.");
            }

            if (roadmap.PublishedVersion is null)
            {
                throw new ConflictException(
                    "Roadmap does not have a published version.");
            }

            var roadmapSkills = await _dbContext.RoadmapNodeSkills
                .AsNoTracking()
                .Where(x =>
                    x.RoadmapNode.RoadmapVersionId ==
                    roadmap.PublishedVersion.RoadmapVersionId)
                .Select(x => new
                {
                    x.Skill.SkillId,

                    SkillName = x.Skill.Name,

                    CategoryName = x.Skill.Category,
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.CategoryName))
                .Distinct()
                .ToListAsync();

            var roadmapSkillIds = roadmapSkills
                .Select(x => x.SkillId)
                .ToHashSet();

            var requestedSkillIds = request.SelectedSkillIds ?? new List<Guid>();

            var invalidSkillIds = requestedSkillIds
                .Where(skillId => !roadmapSkillIds.Contains(skillId))
                .ToList();

            if (invalidSkillIds.Count > 0)
            {
                throw new ConflictException(
                    "One or more selected skills do not belong to the selected roadmap.");
            }

            var selectedSkillIds = requestedSkillIds
                .Distinct()
                .ToHashSet();

            var comparedSkills = roadmapSkills
                .Select(skill => new
                {
                    skill.SkillId,

                    skill.SkillName,

                    skill.CategoryName,

                    IsMatched = selectedSkillIds.Contains(skill.SkillId),
                })
                .ToList();

            var categoryConfigs = await _dbContext.SkillGapCategoryConfigs
                .AsNoTracking()
                .Where(x => x.RoadmapId == request.RoadmapId)
                .Select(x => new
                {
                    x.CategoryName,

                    x.DisplayOrder,
                })
                .ToListAsync();

            if (categoryConfigs.Count == 0)
            {
                throw new ConflictException(
                    "Category configuration has not been generated.");
            }

            var categories = categoryConfigs
                .GroupJoin(
                    comparedSkills,
                    config => config.CategoryName,
                    skill => skill.CategoryName,
                    (config, skills) => new AnalysisCategoryDto
                    {
                        CategoryName = config.CategoryName,

                        DisplayOrder = config.DisplayOrder,

                        TotalSkills = skills.Count(),

                        MatchedSkills = skills.Count(x => x.IsMatched),

                        MissingSkills = skills.Count(x => !x.IsMatched),

                        Skills = skills
                            .OrderBy(x => x.SkillName)
                            .Select(x => new AnalysisSkillDto
                            {
                                SkillId = x.SkillId,

                                SkillName = x.SkillName,

                                IsMatched = x.IsMatched,
                            })
                            .ToList()
                    })
                .OrderBy(x => x.DisplayOrder)
                .ToList();

            var totalSkills = comparedSkills.Count;

            var matchedSkills = comparedSkills.Count(x => x.IsMatched);

            var missingSkills = totalSkills - matchedSkills;

                
            var historyId = Guid.NewGuid();

            var response = new AnalyzeSkillGapResponseDto
            {
                SkillGapAnalysisHistoryId = historyId,

                RoadmapId = roadmap.RoadmapId,

                RoadmapName = roadmap.RoadmapName,

                CareerRoleName = roadmap.CareerRoleName,

                RoadmapVersionTitle = roadmap.PublishedVersion.Title,

                RoadmapVersionNumber = roadmap.PublishedVersion.VersionNumber,

                AuthorName = roadmap.AuthorName,

                MatchedSkills = matchedSkills,

                TotalSkills = totalSkills,

                MissingSkills = missingSkills,

                Categories = categories,
            };

            var snapshotJson = JsonSerializer.Serialize(response);

            _dbContext.SkillGapAnalysisHistories.Add(
                new SkillGapAnalysisHistory
                {
                    SkillGapAnalysisHistoryId = historyId,

                    UserId = userId,

                    CareerRoleId = roadmap.CareerRoleId,

                    RoadmapId = roadmap.RoadmapId,

                    RoadmapVersionId =
                        roadmap.PublishedVersion.RoadmapVersionId,

                    CareerRoleNameSnapshot =
                        roadmap.CareerRoleName,

                    RoadmapTitleSnapshot =
                        roadmap.RoadmapName,

                    RoadmapVersionTitleSnapshot =
                        roadmap.PublishedVersion.Title,

                    AuthorNameSnapshot =
                        roadmap.AuthorName,

                    MatchedSkills = matchedSkills,

                    TotalSkills = totalSkills,

                    MissingSkills = missingSkills,

                    SnapshotJson = snapshotJson,

                    CreatedAt = DateTime.UtcNow,

                    IsDeleted = false,
                });

            await _dbContext.SaveChangesAsync();

            return response;
        }
    }
}
