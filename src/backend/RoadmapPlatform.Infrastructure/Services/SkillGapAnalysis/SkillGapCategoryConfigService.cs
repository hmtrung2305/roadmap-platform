using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.SkillGapAnalysis.Catalog;
using RoadmapPlatform.Application.DTOs.SkillGapAnalysis.CategoryConfig;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.SkillGapAnalysis;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Infrastructure.Services.SkillGapAnalysis
{
    public class SkillGapCategoryConfigService : ISkillGapCategoryConfigService
    {
        private readonly ApplicationDbContext _dbContext;

        public SkillGapCategoryConfigService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task GenerateCategoryConfigurationAsync(Guid roadmapId)
        {
            var roadmapExists = await _dbContext.Roadmaps
                    .AsNoTracking()
                    .AnyAsync(x => x.RoadmapId == roadmapId);

            if (!roadmapExists)
            {
                throw new NotFoundException("Roadmap not found.");
            }

            var publishedVersion = await _dbContext.RoadmapVersions
                .AsNoTracking()
                .Where(x =>
                    x.RoadmapId == roadmapId &&
                    x.Status == "published")
                .Select(x => new
                {
                    x.RoadmapVersionId,
                })
                .SingleOrDefaultAsync();

            if (publishedVersion is null)
            {
                throw new ConflictException("Roadmap does not have a published version.");
            }

            var existingConfigs = await _dbContext.SkillGapCategoryConfigs
                .Where(x => x.RoadmapVersionId == publishedVersion.RoadmapVersionId)
                .ToListAsync();

            if (existingConfigs.Count > 0)
            {
                _dbContext.SkillGapCategoryConfigs.RemoveRange(existingConfigs);
            }

            var categories = await _dbContext.RoadmapNodeSkills
                .AsNoTracking()
                .Where(x => x.RoadmapNode.RoadmapVersionId == publishedVersion.RoadmapVersionId)
                .Select(x => x.Skill.Category)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            var displayOrder = 1;

            foreach (var category in categories)
            {
                _dbContext.SkillGapCategoryConfigs.Add(new SkillGapCategoryConfig
                {
                    SkillGapCategoryConfigId = Guid.NewGuid(),

                    RoadmapId = roadmapId,

                    RoadmapVersionId = publishedVersion.RoadmapVersionId,

                    CategoryName = category!,

                    DisplayOrder = displayOrder++,

                    CreatedAt = DateTime.UtcNow,

                    UpdatedAt = DateTime.UtcNow,
                });
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task<CategoryConfigurationResponseDto> GetCategoryConfigurationAsync(Guid actorUserId, Guid roadmapId)
        {
            await EnsureRoadmapOwnedByActorAsync(
                roadmapId,
                actorUserId);

            var roadmap = await _dbContext.Roadmaps
                .AsNoTracking()
                .Where(x => x.RoadmapId == roadmapId)
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

            var categoryConfigs = await _dbContext.SkillGapCategoryConfigs
                .AsNoTracking()
                .Where(x => x.RoadmapVersionId == roadmap.PublishedVersion.RoadmapVersionId)
                .OrderBy(x => x.DisplayOrder)
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

            var response = new CategoryConfigurationResponseDto
            {
                RoadmapId = roadmap.RoadmapId,

                RoadmapName = roadmap.RoadmapName,

                CareerRoleName = roadmap.CareerRoleName,

                AuthorName = roadmap.AuthorName,

                RoadmapVersionTitle = roadmap.PublishedVersion.Title,

                RoadmapVersionNumber = roadmap.PublishedVersion.VersionNumber,

                Categories = categoryConfigs
                    .GroupJoin(
                        skills,
                        config => config.CategoryName,
                        skill => skill.CategoryName,
                        (config, categorySkills) => new CategoryConfigurationDto
                        {
                            CategoryName = config.CategoryName,

                            DisplayOrder = config.DisplayOrder,

                            TotalSkills = categorySkills.Count(),

                            Skills = categorySkills
                                .OrderBy(x => x.SkillName)
                                .Select(x => new CategorySkillDto
                                {
                                    SkillId = x.SkillId,

                                    SkillName = x.SkillName,
                                })
                                .ToList(),
                        })
                    .OrderBy(x => x.DisplayOrder)
                    .ToList(),
            };

            return response;
        }

        public async Task<List<PublishedRoadmapOptionDto>> GetMyPublishedRoadmapsAsync(Guid userId)
        {
            return await _dbContext.Roadmaps
                    .AsNoTracking()
                    .Where(x =>
                        x.OwnerUserId == userId &&
                        x.RoadmapVersions.Any(v => v.Status == "published"))
                    .Select(x => new PublishedRoadmapOptionDto
                    {
                        RoadmapId = x.RoadmapId,

                        RoadmapName = x.Title,

                        CareerRoleName = x.CareerRole.Name,

                        RoadmapVersionTitle = x.RoadmapVersions
                            .Where(v => v.Status == "published")
                            .Select(v => v.Title)
                            .Single(),

                        RoadmapVersionNumber = x.RoadmapVersions
                            .Where(v => v.Status == "published")
                            .Select(v => v.VersionNumber)
                            .Single()
                    })
                    .OrderBy(x => x.CareerRoleName)
                    .ThenBy(x => x.RoadmapName)
                    .ToListAsync();
        }

        public async Task UpdateCategoryDisplayOrderAsync(Guid actorUserId, Guid roadmapId, List<UpdateCategoryDisplayOrderDto> request)
        {
            await EnsureRoadmapOwnedByActorAsync(
                roadmapId,
                actorUserId);

            var publishedVersion = await _dbContext.Roadmaps
                    .AsNoTracking()
                    .Where(x => x.RoadmapId == roadmapId)
                    .Select(x => x.RoadmapVersions
                        .Where(v => v.Status == "published")
                        .Select(v => new
                        {
                            v.RoadmapVersionId,
                        })
                        .SingleOrDefault())
                    .SingleOrDefaultAsync();

            if (publishedVersion is null)
            {
                throw new ConflictException("Roadmap does not have a published version.");
            }

            if (request == null || request.Count == 0)
            {
                throw new ConflictException("Category configuration is required.");
            }

            if (request.GroupBy(x => x.CategoryName).Any(x => x.Count() > 1))
            {
                throw new ConflictException("Duplicate category name.");
            }

            if (request.GroupBy(x => x.DisplayOrder).Any(x => x.Count() > 1))
            {
                throw new ConflictException("Duplicate display order.");
            }

            var actualOrders = request
                .Select(x => x.DisplayOrder)
                .OrderBy(x => x)
                .ToList();

            var expectedOrders = Enumerable
                .Range(1, request.Count)
                .ToList();

            if (!actualOrders.SequenceEqual(expectedOrders))
            {
                throw new ConflictException(
                    "Display order must be consecutive starting from 1.");
            }

            var configs = await _dbContext.SkillGapCategoryConfigs
                .Where(x => x.RoadmapVersionId == publishedVersion.RoadmapVersionId)
                .ToListAsync();

            if (configs.Count == 0)
            {
                throw new NotFoundException("Category configuration not found.");
            }

            if (configs.Count != request.Count)
            {
                throw new ConflictException("Invalid category configuration.");
            }

            var configDictionary = configs.ToDictionary(x => x.CategoryName);

            foreach (var item in request)
            {
                if (!configDictionary.TryGetValue(item.CategoryName, out var config))
                {
                    throw new ConflictException(
                        $"Category '{item.CategoryName}' does not exist.");
                }

                config.DisplayOrder = item.DisplayOrder;
                config.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();
        }

        private async Task EnsureRoadmapOwnedByActorAsync(Guid roadmapId, Guid actorUserId)
        {
            var isOwned = await _dbContext.Roadmaps
                .AsNoTracking()
                .AnyAsync(x =>
                    x.RoadmapId == roadmapId &&
                    x.OwnerUserId == actorUserId);

            if (!isOwned)
            {
                throw new NotFoundException("Roadmap not found.");
            }
        }
    }
}
