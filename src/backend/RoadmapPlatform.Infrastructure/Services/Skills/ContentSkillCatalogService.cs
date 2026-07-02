using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.Skills;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.Skills;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using System.Text;

namespace RoadmapPlatform.Infrastructure.Services.Skills;

public sealed class ContentSkillCatalogService(ApplicationDbContext dbContext) : IContentSkillCatalogService
{
    private const int DefaultLimit = 20;
    private const int MaxLimit = 50;

    public async Task<ContentSkillSearchResultDto> SearchSkillsAsync(
        ContentSkillSearchQueryDto query,
        CancellationToken cancellationToken)
    {
        var safeLimit = Math.Clamp(query.Limit ?? DefaultLimit, 1, MaxLimit);
        var safeOffset = Math.Max(query.Offset ?? 0, 0);

        var skillsQuery = dbContext.Set<Skill>()
            .AsNoTracking()
            .AsQueryable();

        var category = NormalizeOptionalText(query.Category);
        if (category is not null)
        {
            skillsQuery = skillsQuery.Where(skill =>
                skill.Category != null
                && skill.Category.ToLower() == category.ToLower());
        }

        var search = NormalizeOptionalText(query.Search);
        if (search is not null)
        {
            var lowered = search.ToLower();

            skillsQuery = skillsQuery.Where(skill =>
                skill.Name.ToLower().Contains(lowered)
                || skill.Slug.ToLower().Contains(lowered)
                || (skill.Category != null && skill.Category.ToLower().Contains(lowered))
                || (skill.Description != null && skill.Description.ToLower().Contains(lowered)));
        }

        var totalCount = await skillsQuery.CountAsync(cancellationToken);

        var rows = await skillsQuery
            .OrderByDescending(skill => skill.UpdatedAt)
            .ThenByDescending(skill => skill.CreatedAt)
            .ThenBy(skill => skill.Name)
            .Skip(safeOffset)
            .Take(safeLimit)
            .Select(skill => new SkillUsageRow
            {
                SkillId = skill.SkillId,
                Name = skill.Name,
                Slug = skill.Slug,
                Description = skill.Description,
                Category = skill.Category,
                IsActive = skill.IsActive,
                RoadmapNodeSkillCount = skill.RoadmapNodeSkills.Count,
                SkillModuleCount = skill.SkillModules.Count,
                CreatedAt = skill.CreatedAt,
                UpdatedAt = skill.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new ContentSkillSearchResultDto
        {
            Items = rows.Select(MapSkill).ToList(),
            TotalCount = totalCount,
            Limit = safeLimit,
            Offset = safeOffset
        };
    }

    public async Task<ContentSkillDto> GetSkillAsync(
        Guid skillId,
        CancellationToken cancellationToken)
    {
        var row = await dbContext.Set<Skill>()
            .AsNoTracking()
            .Where(skill => skill.SkillId == skillId)
            .Select(skill => new SkillUsageRow
            {
                SkillId = skill.SkillId,
                Name = skill.Name,
                Slug = skill.Slug,
                Description = skill.Description,
                Category = skill.Category,
                IsActive = skill.IsActive,
                RoadmapNodeSkillCount = skill.RoadmapNodeSkills.Count,
                SkillModuleCount = skill.SkillModules.Count,
                CreatedAt = skill.CreatedAt,
                UpdatedAt = skill.UpdatedAt
            })
            .SingleOrDefaultAsync(cancellationToken);

        return row is null
            ? throw new NotFoundException("Skill was not found.")
            : MapSkill(row);
    }

    public async Task<ContentSkillDto> CreateSkillAsync(
        CreateContentSkillRequestDto request,
        CancellationToken cancellationToken)
    {
        var name = NormalizeRequiredText(request.Name, "Skill name is required.");
        EnsureMaxLength(name, 100, "Skill name cannot exceed 100 characters.");

        var category = await NormalizeCategoryAsync(request.Category, cancellationToken);
        EnsureMaxLength(category, 100, "Skill category cannot exceed 100 characters.");

        var description = NormalizeOptionalText(request.Description);
        var slug = Slugify(name);
        EnsureMaxLength(slug, 120, "Skill slug cannot exceed 120 characters.");

        await EnsureSkillNameAvailableAsync(name, null, cancellationToken);
        await EnsureSkillSlugAvailableAsync(slug, null, cancellationToken);

        var now = DateTime.UtcNow;
        var skill = new Skill
        {
            SkillId = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            Description = description,
            Category = category,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Set<Skill>().Add(skill);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetSkillAsync(skill.SkillId, cancellationToken);
    }

    public async Task<ContentSkillDto> UpdateSkillAsync(
        Guid skillId,
        UpdateContentSkillRequestDto request,
        CancellationToken cancellationToken)
    {
        var skill = await dbContext.Set<Skill>()
            .Where(item => item.SkillId == skillId)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Skill was not found.");

        var usageCount = await GetSkillUsageCountAsync(skillId, cancellationToken);
        if (usageCount > 0)
        {
            throw new ConflictException("This skill is already used and cannot be edited by a content manager.");
        }

        var name = NormalizeRequiredText(request.Name, "Skill name is required.");
        EnsureMaxLength(name, 100, "Skill name cannot exceed 100 characters.");

        var category = await NormalizeCategoryAsync(request.Category, cancellationToken);
        EnsureMaxLength(category, 100, "Skill category cannot exceed 100 characters.");

        var slug = Slugify(name);
        EnsureMaxLength(slug, 120, "Skill slug cannot exceed 120 characters.");

        await EnsureSkillNameAvailableAsync(name, skillId, cancellationToken);
        await EnsureSkillSlugAvailableAsync(slug, skillId, cancellationToken);

        skill.Name = name;
        skill.Slug = slug;
        skill.Description = NormalizeOptionalText(request.Description);
        skill.Category = category;
        skill.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetSkillAsync(skill.SkillId, cancellationToken);
    }

    private async Task<string> NormalizeCategoryAsync(
        string? value,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizeRequiredText(value, "Skill category is required.");

        var lowered = normalized.ToLower();
        var existing = await dbContext.Set<Skill>()
            .AsNoTracking()
            .Where(skill => skill.Category != null && skill.Category.ToLower() == lowered)
            .Select(skill => skill.Category)
            .FirstOrDefaultAsync(cancellationToken);

        return NormalizeOptionalText(existing) ?? normalized;
    }

    private async Task EnsureSkillNameAvailableAsync(
        string name,
        Guid? currentSkillId,
        CancellationToken cancellationToken)
    {
        var normalized = name.Trim().ToLowerInvariant();

        var exists = await dbContext.Set<Skill>()
            .AsNoTracking()
            .AnyAsync(skill =>
                skill.Name.ToLower() == normalized
                && (!currentSkillId.HasValue || skill.SkillId != currentSkillId.Value),
                cancellationToken);

        if (exists)
        {
            throw new ConflictException("A skill with this name already exists.");
        }
    }

    private async Task EnsureSkillSlugAvailableAsync(
        string slug,
        Guid? currentSkillId,
        CancellationToken cancellationToken)
    {
        var normalized = slug.Trim().ToLowerInvariant();

        var exists = await dbContext.Set<Skill>()
            .AsNoTracking()
            .AnyAsync(skill =>
                skill.Slug.ToLower() == normalized
                && (!currentSkillId.HasValue || skill.SkillId != currentSkillId.Value),
                cancellationToken);

        if (exists)
        {
            throw new ConflictException("A skill with this slug already exists.");
        }
    }

    private async Task<int> GetSkillUsageCountAsync(
        Guid skillId,
        CancellationToken cancellationToken)
    {
        var roadmapNodeUsage = await dbContext.Set<RoadmapNodeSkill>()
            .AsNoTracking()
            .CountAsync(mapping => mapping.SkillId == skillId, cancellationToken);

        var moduleUsage = await dbContext.Set<SkillModule>()
            .AsNoTracking()
            .CountAsync(module => module.SkillId == skillId, cancellationToken);

        return roadmapNodeUsage + moduleUsage;
    }

    private static ContentSkillDto MapSkill(SkillUsageRow row)
    {
        var usageCount = row.RoadmapNodeSkillCount + row.SkillModuleCount;

        return new ContentSkillDto
        {
            SkillId = row.SkillId,
            Name = row.Name,
            Slug = row.Slug,
            Description = row.Description,
            Category = row.Category,
            IsActive = row.IsActive,
            UsageCount = usageCount,
            CanEdit = usageCount == 0,
            CreatedAt = row.CreatedAt,
            UpdatedAt = row.UpdatedAt
        };
    }

    private static string NormalizeRequiredText(string? value, string errorMessage)
    {
        var normalized = NormalizeOptionalText(value);
        return normalized ?? throw new ArgumentException(errorMessage);
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static void EnsureMaxLength(string? value, int maxLength, string errorMessage)
    {
        if (value is not null && value.Length > maxLength)
        {
            throw new ArgumentException(errorMessage);
        }
    }

    private static string Slugify(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        var builder = new StringBuilder(normalized.Length);
        var previousDash = false;

        foreach (var character in normalized)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousDash = false;
            }
            else if (!previousDash)
            {
                builder.Append('-');
                previousDash = true;
            }
        }

        var slug = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "skill" : slug;
    }

    private sealed class SkillUsageRow
    {
        public Guid SkillId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; }
        public int RoadmapNodeSkillCount { get; set; }
        public int SkillModuleCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
