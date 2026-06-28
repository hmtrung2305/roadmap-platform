using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.Roadmaps.ContentManagement;
using RoadmapPlatform.Application.DTOs.Roadmaps;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services.Roadmaps;

namespace RoadmapPlatform.Infrastructure.Services.Roadmaps.ContentManagement;

public sealed class ContentManagerRoadmapQueryService(ApplicationDbContext dbContext)
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<ContentRoadmapListResultDto> GetRoadmapsAsync(
        ContentRoadmapListQueryDto query,
        CancellationToken cancellationToken)
    {
        var safePage = Math.Max(query.Page, 1);
        var safePageSize = Math.Clamp(query.PageSize <= 0 ? DefaultPageSize : query.PageSize, 1, MaxPageSize);
        var status = ContentManagerRoadmapText.NormalizeOptionalText(query.Status)?.ToLowerInvariant();
        var sort = ContentManagerRoadmapText.NormalizeOptionalText(query.Sort)?.ToLowerInvariant() ?? "updated_desc";

        if (status is not null and not ("draft" or "published" or "archived"))
        {
            throw new ArgumentException("Unsupported roadmap status.", nameof(query.Status));
        }

        if (sort is not ("updated_desc" or "created_desc" or "title_asc" or "title_desc"))
        {
            throw new ArgumentException("Unsupported roadmap sort value.", nameof(query.Sort));
        }

        var roadmapsQuery = dbContext.Set<Roadmap>()
            .AsNoTracking()
            .Include(roadmap => roadmap.CareerRole)
            .Include(roadmap => roadmap.RoadmapVersions)
            .AsQueryable();

        var search = ContentManagerRoadmapText.NormalizeOptionalText(query.Search);
        if (search is not null)
        {
            var pattern = ContentManagerRoadmapText.BuildContainsPattern(search);

            roadmapsQuery = roadmapsQuery.Where(roadmap =>
                EF.Functions.ILike(roadmap.Title, pattern, "\\")
                || (roadmap.Description != null && EF.Functions.ILike(roadmap.Description, pattern, "\\"))
                || EF.Functions.ILike(roadmap.CareerRole.Name, pattern, "\\")
                || EF.Functions.ILike(roadmap.CareerRole.Slug, pattern, "\\"));
        }

        var roadmaps = await roadmapsQuery.ToListAsync(cancellationToken);

        var statusRows = roadmaps
            .SelectMany(roadmap => roadmap.RoadmapVersions
                .GroupBy(version => version.Status)
                .Select(group => new RoadmapVersionRow(roadmap, RoadmapVersionLabels.OrderNewestFirst(group).First())))
            .GroupBy(row => row.Version.Status)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        var filteredRows = roadmaps
            .Select(roadmap => new
            {
                Roadmap = roadmap,
                Version = status is null
                    ? RoadmapVersionLabels.OrderNewestFirst(roadmap.RoadmapVersions).FirstOrDefault()
                    : RoadmapVersionLabels.OrderNewestFirst(roadmap.RoadmapVersions
                        .Where(version => version.Status.Equals(status, StringComparison.OrdinalIgnoreCase)))
                        .FirstOrDefault()
            })
            .Where(row => row.Version != null)
            .Select(row => new RoadmapVersionRow(row.Roadmap, row.Version!))
            .ToList();

        filteredRows = sort switch
        {
            "created_desc" => filteredRows
                .OrderByDescending(row => row.Version.CreatedAt)
                .ThenBy(row => row.Version.Title)
                .ToList(),
            "title_asc" => filteredRows
                .OrderBy(row => row.Version.Title)
                .ThenByDescending(row => row.Version.MajorVersion)
                .ThenByDescending(row => row.Version.MinorVersion)
                .ThenByDescending(row => row.Version.PatchVersion)
                .ThenByDescending(row => row.Version.VersionNumber)
                .ToList(),
            "title_desc" => filteredRows
                .OrderByDescending(row => row.Version.Title)
                .ThenByDescending(row => row.Version.MajorVersion)
                .ThenByDescending(row => row.Version.MinorVersion)
                .ThenByDescending(row => row.Version.PatchVersion)
                .ThenByDescending(row => row.Version.VersionNumber)
                .ToList(),
            _ => filteredRows
                .OrderByDescending(row => row.Version.UpdatedAt)
                .ThenBy(row => row.Version.Title)
                .ToList()
        };

        var totalCount = filteredRows.Count;
        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)safePageSize);
        var effectivePage = totalPages == 0
            ? 1
            : Math.Min(safePage, totalPages);

        var pageRows = filteredRows
            .Skip((effectivePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();

        var versionIds = pageRows.Select(row => row.Version.RoadmapVersionId).ToList();
        var aggregates = await LoadAggregatesAsync(versionIds, cancellationToken);

        return new ContentRoadmapListResultDto
        {
            Items = pageRows
                .Select(row => ContentManagerRoadmapMapper.MapSummary(row.Roadmap, row.Version, aggregates.GetValueOrDefault(row.Version.RoadmapVersionId)))
                .ToList(),
            TotalCount = totalCount,
            Page = effectivePage,
            PageSize = safePageSize,
            TotalPages = totalPages,
            StatusCounts = new ContentRoadmapStatusCountsDto
            {
                Draft = GetStatusCount(statusRows, "draft"),
                Published = GetStatusCount(statusRows, "published"),
                Archived = GetStatusCount(statusRows, "archived")
            }
        };
    }

    public async Task<ContentRoadmapDetailDto> GetRoadmapDetailAsync(
        Guid roadmapId,
        Guid? roadmapVersionId,
        CancellationToken cancellationToken)
    {
        if (roadmapId == Guid.Empty)
        {
            throw new ArgumentException("Roadmap was not provided.", nameof(roadmapId));
        }

        var roadmap = await dbContext.Set<Roadmap>()
            .AsNoTracking()
            .Include(item => item.CareerRole)
            .Include(item => item.RoadmapVersions)
            .Where(item => item.RoadmapId == roadmapId)
            .FirstOrDefaultAsync(cancellationToken);

        if (roadmap == null)
        {
            throw new KeyNotFoundException("Roadmap was not found.");
        }

        var version = roadmapVersionId.HasValue
            ? roadmap.RoadmapVersions.FirstOrDefault(item => item.RoadmapVersionId == roadmapVersionId.Value)
            : RoadmapVersionLabels.OrderNewestFirst(roadmap.RoadmapVersions)
                .FirstOrDefault();

        if (version == null)
        {
            throw new KeyNotFoundException("Roadmap version was not found.");
        }

        var nodes = await LoadNodesAsync(version.RoadmapVersionId, cancellationToken);
        var edges = await LoadEdgesAsync(version.RoadmapVersionId, cancellationToken);
        var aggregate = new RoadmapVersionAggregate(
            nodes.Count,
            nodes.Count(node => node.IsTrackable),
            nodes.Sum(node => node.Resources.Count),
            nodes.Sum(node => node.Skills.Count));

        return ContentManagerRoadmapMapper.MapDetail(roadmap, version, nodes, edges, aggregate);
    }

    public async Task<ContentRoadmapNodeDto> LoadNodeAsync(
        Guid roadmapNodeId,
        CancellationToken cancellationToken)
    {
        var node = await dbContext.Set<RoadmapNode>()
            .AsNoTracking()
            .Where(item => item.RoadmapNodeId == roadmapNodeId)
            .FirstOrDefaultAsync(cancellationToken);

        if (node == null)
        {
            throw new KeyNotFoundException("Roadmap node was not found.");
        }

        var skillsByNodeId = await LoadSkillsByNodeIdAsync([roadmapNodeId], cancellationToken);
        var resourcesByNodeId = await LoadResourcesByNodeIdAsync([roadmapNodeId], cancellationToken);

        return ContentManagerRoadmapMapper.MapNode(
            node,
            skillsByNodeId.GetValueOrDefault(roadmapNodeId) ?? [],
            resourcesByNodeId.GetValueOrDefault(roadmapNodeId) ?? []);
    }

    private async Task<List<ContentRoadmapNodeDto>> LoadNodesAsync(
        Guid roadmapVersionId,
        CancellationToken cancellationToken)
    {
        var nodes = await dbContext.Set<RoadmapNode>()
            .AsNoTracking()
            .Where(node => node.RoadmapVersionId == roadmapVersionId)
            .OrderBy(node => node.OrderIndex)
            .ThenBy(node => node.Title)
            .ToListAsync(cancellationToken);

        var nodeIds = nodes.Select(node => node.RoadmapNodeId).ToList();
        var skillsByNodeId = await LoadSkillsByNodeIdAsync(nodeIds, cancellationToken);
        var resourcesByNodeId = await LoadResourcesByNodeIdAsync(nodeIds, cancellationToken);

        return nodes
            .Select(node => ContentManagerRoadmapMapper.MapNode(
                node,
                skillsByNodeId.GetValueOrDefault(node.RoadmapNodeId) ?? [],
                resourcesByNodeId.GetValueOrDefault(node.RoadmapNodeId) ?? []))
            .ToList();
    }

    private async Task<List<ContentRoadmapEdgeDto>> LoadEdgesAsync(
        Guid roadmapVersionId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<RoadmapEdge>()
            .AsNoTracking()
            .Where(edge => edge.RoadmapVersionId == roadmapVersionId)
            .OrderBy(edge => edge.EdgeType)
            .ThenBy(edge => edge.RoadmapEdgeId)
            .Select(edge => new ContentRoadmapEdgeDto
            {
                RoadmapEdgeId = edge.RoadmapEdgeId,
                RoadmapVersionId = edge.RoadmapVersionId,
                FromNodeId = edge.FromNodeId,
                ToNodeId = edge.ToNodeId,
                EdgeType = edge.EdgeType,
                DependencyType = edge.DependencyType,
                Condition = edge.Condition
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, List<SkillDto>>> LoadSkillsByNodeIdAsync(
        IReadOnlyCollection<Guid> nodeIds,
        CancellationToken cancellationToken)
    {
        if (nodeIds.Count == 0)
        {
            return [];
        }

        var rows = await dbContext.Set<RoadmapNodeSkill>()
            .AsNoTracking()
            .Include(mapping => mapping.Skill)
            .Where(mapping => nodeIds.Contains(mapping.RoadmapNodeId))
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(mapping => mapping.RoadmapNodeId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(mapping => mapping.Skill.Name)
                    .Select(mapping => new SkillDto
                    {
                        SkillId = mapping.Skill.SkillId,
                        Name = mapping.Skill.Name,
                        Slug = mapping.Skill.Slug,
                        Category = mapping.Skill.Category
                    })
                    .ToList());
    }

    private async Task<Dictionary<Guid, List<LearningResourceDto>>> LoadResourcesByNodeIdAsync(
        IReadOnlyCollection<Guid> nodeIds,
        CancellationToken cancellationToken)
    {
        if (nodeIds.Count == 0)
        {
            return [];
        }

        var rows = await dbContext.Set<RoadmapNodeResource>()
            .AsNoTracking()
            .Include(mapping => mapping.LearningResource)
            .Where(mapping => nodeIds.Contains(mapping.RoadmapNodeId))
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(mapping => mapping.RoadmapNodeId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .GroupBy(mapping => NormalizeResourceUrl(mapping.LearningResource.Url))
                    .Select(urlGroup => urlGroup
                        .OrderBy(mapping => mapping.LearningResource.Title)
                        .First())
                    .OrderBy(mapping => mapping.LearningResource.ResourceType)
                    .ThenBy(mapping => mapping.LearningResource.Title)
                    .Select(mapping => new LearningResourceDto
                    {
                        ResourceId = mapping.LearningResource.LearningResourceId,
                        Title = mapping.LearningResource.Title,
                        Url = mapping.LearningResource.Url,
                        ResourceType = mapping.LearningResource.ResourceType,
                        Description = mapping.LearningResource.Description,
                        Provider = mapping.LearningResource.Provider,
                        DifficultyLevel = mapping.LearningResource.DifficultyLevel,
                        LanguageCode = mapping.LearningResource.LanguageCode
                    })
                    .ToList());
    }

    private static string NormalizeResourceUrl(string? url)
    {
        return string.IsNullOrWhiteSpace(url)
            ? string.Empty
            : url.Trim().TrimEnd('/').ToLowerInvariant();
    }

    private async Task<Dictionary<Guid, RoadmapVersionAggregate>> LoadAggregatesAsync(
        IReadOnlyCollection<Guid> versionIds,
        CancellationToken cancellationToken)
    {
        if (versionIds.Count == 0)
        {
            return [];
        }

        var nodeRows = await dbContext.Set<RoadmapNode>()
            .AsNoTracking()
            .Where(node => versionIds.Contains(node.RoadmapVersionId))
            .Select(node => new
            {
                node.RoadmapVersionId,
                node.RoadmapNodeId,
                node.IsTrackable
            })
            .ToListAsync(cancellationToken);

        var nodeIds = nodeRows.Select(node => node.RoadmapNodeId).ToList();
        var resourceCounts = await dbContext.Set<RoadmapNodeResource>()
            .AsNoTracking()
            .Where(mapping => nodeIds.Contains(mapping.RoadmapNodeId))
            .GroupBy(mapping => mapping.RoadmapNodeId)
            .Select(group => new { RoadmapNodeId = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var skillCounts = await dbContext.Set<RoadmapNodeSkill>()
            .AsNoTracking()
            .Where(mapping => nodeIds.Contains(mapping.RoadmapNodeId))
            .GroupBy(mapping => mapping.RoadmapNodeId)
            .Select(group => new { RoadmapNodeId = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var resourceCountsByNodeId = resourceCounts.ToDictionary(item => item.RoadmapNodeId, item => item.Count);
        var skillCountsByNodeId = skillCounts.ToDictionary(item => item.RoadmapNodeId, item => item.Count);

        return nodeRows
            .GroupBy(node => node.RoadmapVersionId)
            .ToDictionary(
                group => group.Key,
                group => new RoadmapVersionAggregate(
                    group.Count(),
                    group.Count(node => node.IsTrackable),
                    group.Sum(node => resourceCountsByNodeId.GetValueOrDefault(node.RoadmapNodeId)),
                    group.Sum(node => skillCountsByNodeId.GetValueOrDefault(node.RoadmapNodeId))));
    }

    private static int GetStatusCount(Dictionary<string, int> counts, string status)
    {
        return counts.TryGetValue(status, out var count) ? count : 0;
    }

    private sealed record RoadmapVersionRow(Roadmap Roadmap, RoadmapVersion Version);
}
