using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.SkillGapAnalysis.Assessment;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.SkillGapAnalysis;
using RoadmapPlatform.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Infrastructure.Services.SkillGapAnalysis
{
    public class SkillGapAssessmentService : ISkillGapAssessmentService
    {
        private readonly ApplicationDbContext _dbContext;
        public SkillGapAssessmentService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<AssessmentResponseDto> GetAssessmentAsync(Guid roadmapId)
        {
            var roadmap = await _dbContext.Roadmaps
                .AsNoTracking()
                .Where(x =>
                    x.RoadmapId == roadmapId &&
                    x.Visibility == "public")
                .Select(x => new
                {
                    x.RoadmapId,

                    RoadmapName = x.Title,

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
                        .SingleOrDefault(),
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

            var skills = await _dbContext.RoadmapNodeSkills
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

            var categoryConfigs = await _dbContext.SkillGapCategoryConfigs
                .AsNoTracking()
                .Where(x => x.RoadmapVersionId == roadmap.PublishedVersion.RoadmapVersionId)
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
                    skills,
                    config => config.CategoryName,
                    skill => skill.CategoryName,
                    (config, categorySkills) => new AssessmentCategoryDto
                    {
                        CategoryName = config.CategoryName,

                        DisplayOrder = config.DisplayOrder,

                        Skills = categorySkills
                            .OrderBy(x => x.SkillName)
                            .Select(x => new AssessmentSkillDto
                            {
                                SkillId = x.SkillId,
                                SkillName = x.SkillName,
                            })
                            .ToList(),
                    })
                .OrderBy(x => x.DisplayOrder)
                .ToList();

            return new AssessmentResponseDto
            {
                RoadmapId = roadmap.RoadmapId,

                RoadmapName = roadmap.RoadmapName,

                CareerRoleName = roadmap.CareerRoleName,

                RoadmapVersionTitle = roadmap.PublishedVersion.Title,

                RoadmapVersionNumber = roadmap.PublishedVersion.VersionNumber,

                AuthorName = roadmap.AuthorName,

                Categories = categories,
            };
        }
    }
}
