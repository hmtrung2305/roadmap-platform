using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.Skills;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.Skills;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.Skills;

public sealed class SkillLookupService : ISkillLookupService
{
    private const int DefaultLimit = 20;
    private const int MaxLimit = 50;
    private const int DefaultSuggestionLimit = 6;
    private const int MaxSuggestionLimit = 12;
    private const string PublishedVersionStatus = "published";
    private const string PublicRoadmapVisibility = "public";

    private readonly ApplicationDbContext _context;

    public SkillLookupService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SkillSearchResultDto> SearchSkillsAsync(
        string? search,
        string? category,
        int? limit,
        int? offset,
        CancellationToken cancellationToken)
    {
        var safeLimit = Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit);
        var safeOffset = Math.Max(offset ?? 0, 0);

        var query = BuildActiveSkillQuery();

        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalizedCategory = category.Trim().ToLowerInvariant();

            query = query.Where(skill =>
                skill.Category != null
                && skill.Category.ToLower() == normalizedCategory);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLowerInvariant();

            query = query.Where(skill =>
                skill.Name.ToLower().Contains(normalizedSearch)
                || skill.Slug.ToLower().Contains(normalizedSearch)
                || (skill.Category != null && skill.Category.ToLower().Contains(normalizedSearch))
                || skill.RoadmapNodeSkills.Any(mapping =>
                    mapping.RoadmapNode.RoadmapVersion.Status == PublishedVersionStatus
                    && mapping.RoadmapNode.RoadmapVersion.Roadmap.Visibility == PublicRoadmapVisibility
                    && mapping.RoadmapNode.RoadmapVersion.Roadmap.CareerRole.IsActive
                    && mapping.RoadmapNode.RoadmapVersion.Roadmap.CareerRole.Name
                        .ToLower()
                        .Contains(normalizedSearch)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(skill => skill.Category ?? string.Empty)
            .ThenBy(skill => skill.Name)
            .Skip(safeOffset)
            .Take(safeLimit)
            .Select(skill => new SkillLookupDto
            {
                SkillId = skill.SkillId,
                Name = skill.Name,
                Slug = skill.Slug,
                Category = skill.Category,
                Description = skill.Description
            })
            .ToListAsync(cancellationToken);

        await PopulateCareerRolesAsync(items, cancellationToken);

        return new SkillSearchResultDto
        {
            Items = items,
            TotalCount = totalCount,
            Limit = safeLimit,
            Offset = safeOffset
        };
    }

    public async Task<IReadOnlyList<SkillLookupDto>> GetSuggestionsAsync(
        Guid userId,
        int? limit,
        CancellationToken cancellationToken)
    {
        var safeLimit = Math.Clamp(
            limit ?? DefaultSuggestionLimit,
            1,
            MaxSuggestionLimit);

        var recentUsage = await _context.SkillModules
            .AsNoTracking()
            .Where(module =>
                module.CreatedByUserId == userId
                && module.Skill.IsActive)
            .GroupBy(module => module.SkillId)
            .Select(group => new
            {
                SkillId = group.Key,
                LastUsedAt = group.Max(module => module.UpdatedAt)
            })
            .OrderByDescending(item => item.LastUsedAt)
            .Take(safeLimit)
            .ToListAsync(cancellationToken);

        var recentSkillIds = recentUsage
            .Select(item => item.SkillId)
            .ToArray();

        var suggestions = new List<SkillLookupDto>(safeLimit);

        if (recentSkillIds.Length > 0)
        {
            var recentSkills = await BuildActiveSkillQuery()
                .Where(skill => recentSkillIds.Contains(skill.SkillId))
                .Select(skill => new SkillLookupDto
                {
                    SkillId = skill.SkillId,
                    Name = skill.Name,
                    Slug = skill.Slug,
                    Category = skill.Category,
                    Description = skill.Description
                })
                .ToListAsync(cancellationToken);

            var recentSkillsById = recentSkills
                .ToDictionary(skill => skill.SkillId);

            foreach (var skillId in recentSkillIds)
            {
                if (recentSkillsById.TryGetValue(skillId, out var skill))
                {
                    suggestions.Add(skill);
                }
            }
        }

        var remainingCount = safeLimit - suggestions.Count;

        if (remainingCount > 0)
        {
            var excludedSkillIds = suggestions
                .Select(skill => skill.SkillId)
                .ToArray();

            var fallbackSkills = await BuildActiveSkillQuery()
                .Where(skill => !excludedSkillIds.Contains(skill.SkillId))
                .OrderByDescending(skill => skill.SkillModules.Count)
                .ThenBy(skill => skill.Name)
                .Take(remainingCount)
                .Select(skill => new SkillLookupDto
                {
                    SkillId = skill.SkillId,
                    Name = skill.Name,
                    Slug = skill.Slug,
                    Category = skill.Category,
                    Description = skill.Description
                })
                .ToListAsync(cancellationToken);

            suggestions.AddRange(fallbackSkills);
        }

        await PopulateCareerRolesAsync(suggestions, cancellationToken);

        return suggestions;
    }

    public async Task<SkillLookupDto> GetSkillAsync(
        Guid skillId,
        CancellationToken cancellationToken)
    {
        var skill = await BuildActiveSkillQuery()
            .Where(item => item.SkillId == skillId)
            .Select(item => new SkillLookupDto
            {
                SkillId = item.SkillId,
                Name = item.Name,
                Slug = item.Slug,
                Category = item.Category,
                Description = item.Description
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (skill is null)
        {
            throw new NotFoundException("Skill was not found.");
        }

        await PopulateCareerRolesAsync([skill], cancellationToken);

        return skill;
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync(
        CancellationToken cancellationToken)
    {
        return await BuildActiveSkillQuery()
            .Where(skill => skill.Category != null && skill.Category != "")
            .Select(skill => skill.Category!)
            .Distinct()
            .OrderBy(category => category)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Skill> BuildActiveSkillQuery()
    {
        return _context.Skills
            .AsNoTracking()
            .Where(skill => skill.IsActive);
    }

    private IQueryable<RoadmapNodeSkill> BuildPublishedRoadmapUsageQuery()
    {
        return _context.RoadmapNodeSkills
            .AsNoTracking()
            .Where(mapping =>
                mapping.RoadmapNode.RoadmapVersion.Status == PublishedVersionStatus
                && mapping.RoadmapNode.RoadmapVersion.Roadmap.Visibility == PublicRoadmapVisibility
                && mapping.RoadmapNode.RoadmapVersion.Roadmap.CareerRole.IsActive);
    }

    private async Task PopulateCareerRolesAsync(
        IReadOnlyList<SkillLookupDto> skills,
        CancellationToken cancellationToken)
    {
        if (skills.Count == 0)
        {
            return;
        }

        var skillIds = skills
            .Select(skill => skill.SkillId)
            .Distinct()
            .ToArray();

        var usageRows = await BuildPublishedRoadmapUsageQuery()
            .Where(mapping => skillIds.Contains(mapping.SkillId))
            .Select(mapping => new
            {
                mapping.SkillId,
                CareerRoleName = mapping.RoadmapNode
                    .RoadmapVersion
                    .Roadmap
                    .CareerRole
                    .Name
            })
            .ToListAsync(cancellationToken);

        var rolesBySkillId = usageRows
            .GroupBy(row => row.SkillId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(row => row.CareerRoleName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToArray());

        foreach (var skill in skills)
        {
            skill.CareerRoles = rolesBySkillId.TryGetValue(skill.SkillId, out var roles)
                ? roles
                : [];
        }
    }
}
