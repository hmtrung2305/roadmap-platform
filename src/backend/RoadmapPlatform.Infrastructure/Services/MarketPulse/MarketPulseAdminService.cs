using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.MarketPulse;
using RoadmapPlatform.Application.Interfaces.MarketPulse;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.MarketPulse;

public sealed class MarketPulseAdminService(ApplicationDbContext dbContext) : IMarketPulseAdminService
{
    private static readonly IReadOnlyList<string> DefaultCategories =
    [
        "Backend",
        "Frontend",
        "Fullstack",
        "Mobile",
        "DevOps",
        "Data",
        "AI/ML",
        "QA/Testing",
        "Security",
        "UI/UX",
        "Project/Product Management",
        "Other"
    ];

    public async Task<IReadOnlyList<MarketPulseCrawlRunDto>> GetCrawlRunsAsync(
        MarketPulseAdminQueryDto query,
        CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(query.Limit, 1, 200);
        var runs = dbContext.Set<MarketPulseCrawlRun>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim();
            runs = runs.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Source))
        {
            var source = query.Source.Trim();
            runs = runs.Where(x => x.SourceName == source);
        }

        if (query.From.HasValue)
        {
            runs = runs.Where(x => x.StartedAt >= NormalizeUtc(query.From.Value));
        }

        if (query.To.HasValue)
        {
            runs = runs.Where(x => x.StartedAt <= NormalizeUtc(query.To.Value));
        }

        var rows = await runs
            .OrderByDescending(x => x.StartedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows.Select(ToRunDto).ToList();
    }

    public async Task<IReadOnlyList<MarketPulseFailedItemDto>> GetFailedItemsAsync(
        MarketPulseAdminQueryDto query,
        CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(query.Limit, 1, 200);
        var items = dbContext.Set<MarketPulseFailedItem>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim();
            items = items.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Source))
        {
            var source = query.Source.Trim();
            items = items.Where(x => x.SourceName == source);
        }

        if (query.From.HasValue)
        {
            items = items.Where(x => x.CreatedAt >= NormalizeUtc(query.From.Value));
        }

        if (query.To.HasValue)
        {
            items = items.Where(x => x.CreatedAt <= NormalizeUtc(query.To.Value));
        }

        var rows = await items
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows.Select(ToFailedItemDto).ToList();
    }

    public Task<IReadOnlyList<MarketPulseFailedItemDto>> RetryFailedItemsAsync(
        IReadOnlyCollection<Guid> failedItemIds,
        CancellationToken cancellationToken) =>
        UpdateFailedItemStatusAsync(failedItemIds, "retry_queued", incrementRetry: true, cancellationToken);

    public Task<IReadOnlyList<MarketPulseFailedItemDto>> IgnoreFailedItemsAsync(
        IReadOnlyCollection<Guid> failedItemIds,
        CancellationToken cancellationToken) =>
        UpdateFailedItemStatusAsync(failedItemIds, "ignored", incrementRetry: false, cancellationToken);

    public async Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        var configured = await dbContext.Set<MarketPulseClassifierKeywordMapping>()
            .AsNoTracking()
            .Select(x => x.Category)
            .Distinct()
            .ToListAsync(cancellationToken);

        return DefaultCategories
            .Concat(configured)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x == "Other" ? "zzzz" : x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<MarketPulseClassifierMappingDto>> GetClassifierMappingsAsync(
        CancellationToken cancellationToken)
    {
        await EnsureDefaultClassifierMappingsAsync(cancellationToken);

        var rows = await dbContext.Set<MarketPulseClassifierKeywordMapping>()
            .AsNoTracking()
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Keyword)
            .ToListAsync(cancellationToken);

        return rows.Select(ToMappingDto).ToList();
    }

    public async Task<MarketPulseClassifierMappingDto> CreateClassifierMappingAsync(
        MarketPulseClassifierMappingRequestDto request,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizeMappingRequest(request);
        var now = DateTime.UtcNow;
        var exists = await dbContext.Set<MarketPulseClassifierKeywordMapping>()
            .AnyAsync(x => x.Keyword == normalized.Keyword && x.Category == normalized.Category, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("This classifier keyword mapping already exists.");
        }

        var mapping = new MarketPulseClassifierKeywordMapping
        {
            MarketPulseClassifierKeywordMappingId = Guid.NewGuid(),
            Keyword = normalized.Keyword,
            Category = normalized.Category,
            IsEnabled = normalized.IsEnabled,
            Weight = normalized.Weight,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Set<MarketPulseClassifierKeywordMapping>().Add(mapping);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToMappingDto(mapping);
    }

    public async Task<MarketPulseClassifierMappingDto> UpdateClassifierMappingAsync(
        Guid mappingId,
        MarketPulseClassifierMappingRequestDto request,
        CancellationToken cancellationToken)
    {
        var mapping = await dbContext.Set<MarketPulseClassifierKeywordMapping>()
            .FirstOrDefaultAsync(x => x.MarketPulseClassifierKeywordMappingId == mappingId, cancellationToken);

        if (mapping is null)
        {
            throw new KeyNotFoundException("Classifier mapping was not found.");
        }

        var normalized = NormalizeMappingRequest(request);
        var duplicate = await dbContext.Set<MarketPulseClassifierKeywordMapping>()
            .AnyAsync(x =>
                x.MarketPulseClassifierKeywordMappingId != mappingId &&
                x.Keyword == normalized.Keyword &&
                x.Category == normalized.Category,
                cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException("Another classifier mapping already uses this keyword and category.");
        }

        mapping.Keyword = normalized.Keyword;
        mapping.Category = normalized.Category;
        mapping.IsEnabled = normalized.IsEnabled;
        mapping.Weight = normalized.Weight;
        mapping.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToMappingDto(mapping);
    }

    public async Task DeleteClassifierMappingAsync(Guid mappingId, CancellationToken cancellationToken)
    {
        var mapping = await dbContext.Set<MarketPulseClassifierKeywordMapping>()
            .FirstOrDefaultAsync(x => x.MarketPulseClassifierKeywordMappingId == mappingId, cancellationToken);

        if (mapping is null)
        {
            return;
        }

        dbContext.Set<MarketPulseClassifierKeywordMapping>().Remove(mapping);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<MarketPulseClassifierTestResultDto> TestClassifierAsync(
        MarketPulseClassifierTestRequestDto request,
        CancellationToken cancellationToken)
    {
        await EnsureDefaultClassifierMappingsAsync(cancellationToken);

        var text = NormalizeText(request.Text);
        if (string.IsNullOrWhiteSpace(text))
        {
            return new MarketPulseClassifierTestResultDto
            {
                Category = "Other",
                Confidence = 0,
                FallbackCategory = "Other",
                Matches = []
            };
        }

        var mappings = await dbContext.Set<MarketPulseClassifierKeywordMapping>()
            .AsNoTracking()
            .Where(x => x.IsEnabled)
            .ToListAsync(cancellationToken);

        var matches = mappings
            .Where(x => ContainsKeyword(text, x.Keyword))
            .Select(x => new MarketPulseClassifierMatchDto
            {
                Keyword = x.Keyword,
                Category = x.Category,
                Weight = x.Weight
            })
            .ToList();

        if (matches.Count == 0)
        {
            return new MarketPulseClassifierTestResultDto
            {
                Category = "Other",
                Confidence = 0,
                FallbackCategory = "Other",
                Matches = []
            };
        }

        var totalWeight = matches.Sum(x => x.Weight);
        var best = matches
            .GroupBy(x => x.Category, StringComparer.OrdinalIgnoreCase)
            .Select(x => new
            {
                Category = x.Key,
                Weight = x.Sum(item => item.Weight),
                Count = x.Count()
            })
            .OrderByDescending(x => x.Weight)
            .ThenByDescending(x => x.Count)
            .ThenBy(x => x.Category)
            .First();

        return new MarketPulseClassifierTestResultDto
        {
            Category = best.Category,
            Confidence = totalWeight <= 0 ? 0 : Math.Round(best.Weight / totalWeight, 2),
            FallbackCategory = "Other",
            Matches = matches
                .OrderByDescending(x => x.Weight)
                .ThenBy(x => x.Keyword)
                .ToList()
        };
    }

    public async Task<IReadOnlyList<MarketPulseSourceHealthDto>> GetSourceHealthAsync(
        CancellationToken cancellationToken)
    {
        var healthRows = await dbContext.Set<MarketPulseSourceHealth>()
            .AsNoTracking()
            .OrderBy(x => x.SourceName)
            .ToListAsync(cancellationToken);

        var healthDtos = healthRows.Select(ToSourceHealthDto).ToList();

        var knownSources = await dbContext.Set<JobPortalSource>()
            .AsNoTracking()
            .Select(x => new { x.Name, x.LastScrapedAt, x.IsEnabled })
            .ToListAsync(cancellationToken);

        var bySource = healthDtos.ToDictionary(x => x.Source, StringComparer.OrdinalIgnoreCase);
        foreach (var source in knownSources)
        {
            if (bySource.ContainsKey(source.Name))
            {
                continue;
            }

            bySource[source.Name] = new MarketPulseSourceHealthDto
            {
                Source = source.Name,
                Status = source.IsEnabled ? "unknown" : "disabled",
                LastSuccessAt = source.LastScrapedAt,
                UpdatedAt = source.LastScrapedAt ?? DateTime.UtcNow
            };
        }

        return bySource.Values
            .OrderBy(x => x.Source)
            .ToList();
    }

    private async Task<IReadOnlyList<MarketPulseFailedItemDto>> UpdateFailedItemStatusAsync(
        IReadOnlyCollection<Guid> failedItemIds,
        string status,
        bool incrementRetry,
        CancellationToken cancellationToken)
    {
        var ids = failedItemIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return [];
        }

        var now = DateTime.UtcNow;
        var items = await dbContext.Set<MarketPulseFailedItem>()
            .Where(x => ids.Contains(x.MarketPulseFailedItemId))
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            item.Status = status;
            item.UpdatedAt = now;
            if (incrementRetry)
            {
                item.RetryCount++;
                item.LastRetryAt = now;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return items.Select(ToFailedItemDto).ToList();
    }

    private async Task EnsureDefaultClassifierMappingsAsync(CancellationToken cancellationToken)
    {
        var hasMappings = await dbContext.Set<MarketPulseClassifierKeywordMapping>()
            .AnyAsync(cancellationToken);
        if (hasMappings)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var defaults = new[]
        {
            ("backend", "Backend"),
            ("java", "Backend"),
            ("spring", "Backend"),
            ("asp.net", "Backend"),
            ("frontend", "Frontend"),
            ("react", "Frontend"),
            ("vue", "Frontend"),
            ("angular", "Frontend"),
            ("fullstack", "Fullstack"),
            ("full stack", "Fullstack"),
            ("mobile", "Mobile"),
            ("android", "Mobile"),
            ("ios", "Mobile"),
            ("devops", "DevOps"),
            ("kubernetes", "DevOps"),
            ("data engineer", "Data"),
            ("data analyst", "Data"),
            ("machine learning", "AI/ML"),
            ("ai engineer", "AI/ML"),
            ("qa", "QA/Testing"),
            ("tester", "QA/Testing"),
            ("security", "Security"),
            ("ui ux", "UI/UX"),
            ("product manager", "Project/Product Management"),
            ("project manager", "Project/Product Management")
        };

        dbContext.Set<MarketPulseClassifierKeywordMapping>().AddRange(defaults.Select(item =>
            new MarketPulseClassifierKeywordMapping
            {
                MarketPulseClassifierKeywordMappingId = Guid.NewGuid(),
                Keyword = item.Item1,
                Category = item.Item2,
                IsEnabled = true,
                Weight = 1,
                CreatedAt = now,
                UpdatedAt = now
            }));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static MarketPulseClassifierMappingRequestDto NormalizeMappingRequest(
        MarketPulseClassifierMappingRequestDto request)
    {
        var keyword = NormalizeWhitespace(request.Keyword).ToLowerInvariant();
        var category = NormalizeWhitespace(request.Category);
        var weight = Math.Round(Math.Clamp(request.Weight, 0.1m, 100m), 2);

        if (string.IsNullOrWhiteSpace(keyword))
        {
            throw new ArgumentException("Keyword is required.");
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Category is required.");
        }

        return new MarketPulseClassifierMappingRequestDto
        {
            Keyword = keyword,
            Category = category,
            IsEnabled = request.IsEnabled,
            Weight = weight
        };
    }

    private static MarketPulseCrawlRunDto ToRunDto(MarketPulseCrawlRun run) => new()
    {
        RunId = run.MarketPulseCrawlRunId,
        Source = run.SourceName,
        Status = run.Status,
        Mode = run.Mode,
        TriggerType = run.TriggerType,
        StartedAt = run.StartedAt,
        FinishedAt = run.FinishedAt,
        DurationMs = run.DurationMs,
        FetchedCount = run.FetchedCount,
        SavedCount = run.SavedCount,
        ImportedCount = run.ImportedCount,
        UpdatedCount = run.UpdatedCount,
        SkippedCount = run.SkippedCount,
        DuplicateCount = run.DuplicateCount,
        FailedCount = run.FailedCount,
        StoppedReason = run.StoppedReason,
        ErrorSummary = run.ErrorSummary
    };

    private static MarketPulseFailedItemDto ToFailedItemDto(MarketPulseFailedItem item) => new()
    {
        FailedItemId = item.MarketPulseFailedItemId,
        RunId = item.MarketPulseCrawlRunId,
        Source = item.SourceName,
        Url = item.Url,
        Stage = item.Stage,
        ErrorCode = item.ErrorCode,
        ErrorMessage = item.ErrorMessage,
        RetryCount = item.RetryCount,
        CreatedAt = item.CreatedAt,
        LastRetryAt = item.LastRetryAt,
        Status = item.Status,
        ErrorDetail = item.ErrorDetail
    };

    private static MarketPulseClassifierMappingDto ToMappingDto(
        MarketPulseClassifierKeywordMapping mapping) => new()
    {
        MappingId = mapping.MarketPulseClassifierKeywordMappingId,
        Keyword = mapping.Keyword,
        Category = mapping.Category,
        IsEnabled = mapping.IsEnabled,
        Weight = mapping.Weight,
        CreatedAt = mapping.CreatedAt,
        UpdatedAt = mapping.UpdatedAt
    };

    private static MarketPulseSourceHealthDto ToSourceHealthDto(MarketPulseSourceHealth health) => new()
    {
        Source = health.SourceName,
        Status = health.Status,
        LastSuccessAt = health.LastSuccessAt,
        LastFailureAt = health.LastFailureAt,
        ConsecutiveFailures = health.ConsecutiveFailures,
        LastRunId = health.LastRunId,
        LastErrorSummary = health.LastErrorSummary,
        UpdatedAt = health.UpdatedAt
    };

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    private static bool ContainsKeyword(string normalizedText, string keyword)
    {
        var normalizedKeyword = Regex.Escape(NormalizeText(keyword));
        if (string.IsNullOrWhiteSpace(normalizedKeyword))
        {
            return false;
        }

        return Regex.IsMatch(normalizedText, $@"(^|[^a-z0-9]){normalizedKeyword}([^a-z0-9]|$)");
    }

    private static string NormalizeText(string? value) =>
        RemoveDiacritics(NormalizeWhitespace(value).ToLowerInvariant());

    private static string NormalizeWhitespace(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : string.Join(' ', value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace('\u0111', 'd')
            .Replace('\u0110', 'D');
    }
}
