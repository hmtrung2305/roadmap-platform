using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.LearningResources;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.LearningResources;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.LearningResources;

public sealed class ContentLearningResourceCatalogService(ApplicationDbContext dbContext) : IContentLearningResourceCatalogService
{
    private const int DefaultLimit = 20;
    private const int MaxLimit = 50;
    private const string DefaultLanguageCode = "en";
    private const string DefaultVerificationStatus = "verified";

    private static readonly IReadOnlySet<string> AllowedResourceTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "documentation",
        "video",
        "course",
        "article",
        "book",
        "practice",
        "project",
        "other"
    };

    private static readonly IReadOnlySet<string> AllowedDifficultyLevels = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "beginner",
        "intermediate",
        "advanced"
    };

    public async Task<ContentLearningResourceSearchResultDto> SearchLearningResourcesAsync(
        ContentLearningResourceSearchQueryDto query,
        CancellationToken cancellationToken)
    {
        var safeLimit = Math.Clamp(query.Limit ?? DefaultLimit, 1, MaxLimit);
        var safeOffset = Math.Max(query.Offset ?? 0, 0);

        var resourcesQuery = dbContext.Set<LearningResource>()
            .AsNoTracking()
            .AsQueryable();

        var resourceType = NormalizeOptionalText(query.ResourceType)?.ToLowerInvariant();
        if (resourceType is not null)
        {
            resourcesQuery = resourcesQuery.Where(resource => resource.ResourceType.ToLower() == resourceType);
        }

        var difficultyLevel = NormalizeOptionalText(query.DifficultyLevel)?.ToLowerInvariant();
        if (difficultyLevel is not null)
        {
            resourcesQuery = resourcesQuery.Where(resource =>
                resource.DifficultyLevel != null
                && resource.DifficultyLevel.ToLower() == difficultyLevel);
        }

        var search = NormalizeOptionalText(query.Search);
        if (search is not null)
        {
            var lowered = search.ToLowerInvariant();

            resourcesQuery = resourcesQuery.Where(resource =>
                resource.Title.ToLower().Contains(lowered)
                || resource.Url.ToLower().Contains(lowered)
                || resource.ResourceType.ToLower().Contains(lowered)
                || (resource.Provider != null && resource.Provider.ToLower().Contains(lowered))
                || (resource.Description != null && resource.Description.ToLower().Contains(lowered)));
        }

        var totalCount = await resourcesQuery.CountAsync(cancellationToken);

        var items = await resourcesQuery
            .OrderByDescending(resource => resource.UpdatedAt)
            .ThenByDescending(resource => resource.CreatedAt)
            .ThenBy(resource => resource.Title)
            .Skip(safeOffset)
            .Take(safeLimit)
            .Select(resource => new ContentLearningResourceDto
            {
                ResourceId = resource.LearningResourceId,
                Title = resource.Title,
                Url = resource.Url,
                ResourceType = resource.ResourceType,
                Description = resource.Description,
                Provider = resource.Provider,
                DifficultyLevel = resource.DifficultyLevel,
                LanguageCode = resource.LanguageCode,
                VerificationStatus = resource.VerificationStatus,
                NodeMappingCount = resource.RoadmapNodeResources.Count,
                CreatedAt = resource.CreatedAt,
                UpdatedAt = resource.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new ContentLearningResourceSearchResultDto
        {
            Items = items,
            TotalCount = totalCount,
            Limit = safeLimit,
            Offset = safeOffset
        };
    }

    public async Task<ContentLearningResourceDto> GetLearningResourceAsync(
        Guid learningResourceId,
        CancellationToken cancellationToken)
    {
        var resource = await dbContext.Set<LearningResource>()
            .AsNoTracking()
            .Where(item => item.LearningResourceId == learningResourceId)
            .Select(item => new ContentLearningResourceDto
            {
                ResourceId = item.LearningResourceId,
                Title = item.Title,
                Url = item.Url,
                ResourceType = item.ResourceType,
                Description = item.Description,
                Provider = item.Provider,
                DifficultyLevel = item.DifficultyLevel,
                LanguageCode = item.LanguageCode,
                VerificationStatus = item.VerificationStatus,
                NodeMappingCount = item.RoadmapNodeResources.Count,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            })
            .SingleOrDefaultAsync(cancellationToken);

        return resource ?? throw new NotFoundException("Learning resource was not found.");
    }

    public async Task<ContentLearningResourceDto> CreateLearningResourceAsync(
        CreateContentLearningResourceRequestDto request,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizeRequest(request);
        await EnsureUrlAvailableAsync(normalized.Url, null, cancellationToken);

        var now = DateTime.UtcNow;
        var resource = new LearningResource
        {
            LearningResourceId = Guid.NewGuid(),
            Title = normalized.Title,
            Url = normalized.Url,
            ResourceType = normalized.ResourceType,
            Description = normalized.Description,
            Provider = normalized.Provider,
            DifficultyLevel = normalized.DifficultyLevel,
            LanguageCode = normalized.LanguageCode,
            VerificationStatus = DefaultVerificationStatus,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Set<LearningResource>().Add(resource);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetLearningResourceAsync(resource.LearningResourceId, cancellationToken);
    }

    public async Task<ContentLearningResourceDto> UpdateLearningResourceAsync(
        Guid learningResourceId,
        UpdateContentLearningResourceRequestDto request,
        CancellationToken cancellationToken)
    {
        var resource = await dbContext.Set<LearningResource>()
            .Where(item => item.LearningResourceId == learningResourceId)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Learning resource was not found.");

        var normalized = NormalizeRequest(request);
        var currentNormalizedUrl = NormalizeUrlForComparison(resource.Url);
        var requestedNormalizedUrl = NormalizeUrlForComparison(normalized.Url);

        if (!string.Equals(currentNormalizedUrl, requestedNormalizedUrl, StringComparison.Ordinal))
        {
            await EnsureUrlAvailableAsync(normalized.Url, learningResourceId, cancellationToken);
        }

        resource.Title = normalized.Title;
        resource.Url = normalized.Url;
        resource.ResourceType = normalized.ResourceType;
        resource.Description = normalized.Description;
        resource.Provider = normalized.Provider;
        resource.DifficultyLevel = normalized.DifficultyLevel;
        resource.LanguageCode = normalized.LanguageCode;
        resource.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetLearningResourceAsync(resource.LearningResourceId, cancellationToken);
    }

    private async Task EnsureUrlAvailableAsync(
        string url,
        Guid? currentResourceId,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizeUrlForComparison(url);

        var exists = await dbContext.Set<LearningResource>()
            .AsNoTracking()
            .AnyAsync(resource =>
                resource.Url.Trim().ToLower() == normalized
                && (!currentResourceId.HasValue || resource.LearningResourceId != currentResourceId.Value),
                cancellationToken);

        if (exists)
        {
            throw new ConflictException("A learning resource with this URL already exists.");
        }
    }

    private static NormalizedLearningResourceRequest NormalizeRequest(CreateContentLearningResourceRequestDto request)
    {
        return NormalizeRequest(
            request.Title,
            request.Url,
            request.ResourceType,
            request.Description,
            request.Provider,
            request.DifficultyLevel);
    }

    private static NormalizedLearningResourceRequest NormalizeRequest(UpdateContentLearningResourceRequestDto request)
    {
        return NormalizeRequest(
            request.Title,
            request.Url,
            request.ResourceType,
            request.Description,
            request.Provider,
            request.DifficultyLevel);
    }

    private static NormalizedLearningResourceRequest NormalizeRequest(
        string? titleValue,
        string? urlValue,
        string? resourceTypeValue,
        string? descriptionValue,
        string? providerValue,
        string? difficultyLevelValue)
    {
        var title = NormalizeRequiredText(titleValue, "Learning resource title is required.");
        EnsureMaxLength(title, 200, "Learning resource title cannot exceed 200 characters.");

        var url = NormalizeRequiredText(urlValue, "Learning resource URL is required.");
        EnsureValidHttpUrl(url);

        var resourceType = NormalizeRequiredText(resourceTypeValue, "Resource type is required.").ToLowerInvariant();
        if (!AllowedResourceTypes.Contains(resourceType))
        {
            throw new ArgumentException("Resource type is invalid.");
        }

        var difficultyLevel = NormalizeOptionalText(difficultyLevelValue)?.ToLowerInvariant();
        if (difficultyLevel is not null && !AllowedDifficultyLevels.Contains(difficultyLevel))
        {
            throw new ArgumentException("Difficulty level is invalid.");
        }

        var provider = NormalizeOptionalText(providerValue);
        EnsureMaxLength(provider, 100, "Provider cannot exceed 100 characters.");

        return new NormalizedLearningResourceRequest
        {
            Title = title,
            Url = url,
            ResourceType = resourceType,
            Description = NormalizeOptionalText(descriptionValue),
            Provider = provider,
            DifficultyLevel = difficultyLevel,
            LanguageCode = DefaultLanguageCode
        };
    }

    private static void EnsureValidHttpUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Learning resource URL must be a valid HTTP or HTTPS URL.");
        }
    }

    private static string NormalizeUrlForComparison(string url)
    {
        return url.Trim().ToLowerInvariant();
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

    private sealed class NormalizedLearningResourceRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string ResourceType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Provider { get; set; }
        public string? DifficultyLevel { get; set; }
        public string LanguageCode { get; set; } = string.Empty;
    }
}
