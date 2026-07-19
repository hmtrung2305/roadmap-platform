using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.SkillGapAnalysis.Catalog;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.SkillGapAnalysis;
using RoadmapPlatform.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Infrastructure.Services.SkillGapAnalysis
{
    public class SkillGapCatalogService : ISkillGapCatalogService
    {
        private readonly ApplicationDbContext _dbContext;

        public SkillGapCatalogService(ApplicationDbContext dbContext)
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
                }).ToListAsync();
        }

        public async Task<List<RoadmapOptionDto>> GetPublishedRoadmapsAsync(string careerRoleSlug)
        {
            var careerRoleExists = await _dbContext.CareerRoles
                .AsNoTracking()
                .AnyAsync(x => x.Slug == careerRoleSlug);

            if (!careerRoleExists)
            {
                throw new NotFoundException("Career role not found.");
            }

            return await _dbContext.Roadmaps
                .AsNoTracking()
                .Where(x =>
                    x.CareerRole.Slug == careerRoleSlug &&
                    x.Visibility == "public")
                .Select(roadmap => new
                {
                    Roadmap = roadmap,
                    PublishedVersion = roadmap.RoadmapVersions
                        .FirstOrDefault(version => version.Status == "published")
                })
                .Where(x => x.PublishedVersion != null)
                .OrderByDescending(x => x.PublishedVersion!.PublishedAt)
                .Select(x => new RoadmapOptionDto
                {
                    RoadmapId = x.Roadmap.RoadmapId,

                    PublishedRoadmapVersionId = x.PublishedVersion!.RoadmapVersionId,

                    Slug = x.Roadmap.Slug,

                    Title = x.PublishedVersion.Title,

                    VersionNumber = x.PublishedVersion.VersionNumber,

                    PublishedAt = x.PublishedVersion.PublishedAt,

                    AuthorName = !string.IsNullOrWhiteSpace(
                            x.Roadmap.OwnerUser!.UserProfile!.DisplayName)
                        ? x.Roadmap.OwnerUser.UserProfile.DisplayName!
                        : x.Roadmap.OwnerUser.Username,

                    TotalSkills = x.PublishedVersion.RoadmapNodes
                        .SelectMany(node => node.RoadmapNodeSkills)
                        .Select(skill => skill.SkillId)
                        .Distinct()
                        .Count()
                })
                .ToListAsync();
        }
    }
}
