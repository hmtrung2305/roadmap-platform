using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.Skills;
using RoadmapPlatform.Application.Interfaces.Skills;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.Skills;

public sealed class SkillLookupService : ISkillLookupService
{
    private const int DefaultLimit = 20;
    private const int MaxLimit = 50;

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

        var query = _context.Skills
            .AsNoTracking()
            .Where(skill => skill.IsActive);

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
                || (skill.Category != null && skill.Category.ToLower().Contains(normalizedSearch)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(skill => skill.Category ?? string.Empty)
            .ThenBy(skill => skill.Name)
            .Skip(safeOffset)
            .Take(safeLimit)
            .Select(skill => MapSkill(skill))
            .ToListAsync(cancellationToken);

        return new SkillSearchResultDto
        {
            Items = items,
            TotalCount = totalCount,
            Limit = safeLimit,
            Offset = safeOffset
        };
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync(
        CancellationToken cancellationToken)
    {
        return await _context.Skills
            .AsNoTracking()
            .Where(skill => skill.IsActive && skill.Category != null && skill.Category != "")
            .Select(skill => skill.Category!)
            .Distinct()
            .OrderBy(category => category)
            .ToListAsync(cancellationToken);
    }

    private static SkillLookupDto MapSkill(Skill skill)
    {
        return new SkillLookupDto
        {
            SkillId = skill.SkillId,
            Name = skill.Name,
            Slug = skill.Slug,
            Category = skill.Category,
            Description = skill.Description
        };
    }
}
