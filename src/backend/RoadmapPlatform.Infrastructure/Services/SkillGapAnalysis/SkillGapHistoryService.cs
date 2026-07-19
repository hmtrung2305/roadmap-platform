using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.SkillGapAnalysis.Analysis;
using RoadmapPlatform.Application.DTOs.SkillGapAnalysis.History;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.SkillGapAnalysis;
using RoadmapPlatform.Infrastructure.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace RoadmapPlatform.Infrastructure.Services.SkillGapAnalysis
{
    public class SkillGapHistoryService : ISkillGapHistoryService
    {
        private readonly ApplicationDbContext _dbContext;

        public SkillGapHistoryService(
            ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task DeleteHistoryAsync(
            Guid userId,
            Guid skillGapAnalysisHistoryId,
            CancellationToken cancellationToken)
        {
            var history = await _dbContext.SkillGapAnalysisHistories
                .SingleOrDefaultAsync(
                    x =>
                        x.SkillGapAnalysisHistoryId ==
                        skillGapAnalysisHistoryId &&
                        x.UserId == userId,
                    cancellationToken);

            if (history is null)
            {
                throw new NotFoundException(
                    "Skill gap analysis history not found.");
            }

            _dbContext.SkillGapAnalysisHistories.Remove(history);

            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }

        public async Task<SkillGapHistoryPageDto> GetHistoryAsync(
    Guid userId,
    SkillGapHistoryPageRequestDto request,
    CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.Limit is < 1 or > 50)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(request.Limit),
                    "Limit must be between 1 and 50.");
            }

            var query = _dbContext.SkillGapAnalysisHistories
                .AsNoTracking()
                .Where(x => x.UserId == userId);

            if (!string.IsNullOrWhiteSpace(request.Cursor))
            {
                var cursor = DecodeCursor(request.Cursor);

                query = query.Where(x =>
                    x.CreatedAt < cursor.CreatedAt ||
                    (
                        x.CreatedAt == cursor.CreatedAt &&
                        x.SkillGapAnalysisHistoryId.CompareTo(
                            cursor.HistoryId) < 0
                    ));
            }

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(
                    x => x.SkillGapAnalysisHistoryId)
                .Take(request.Limit + 1)
                .Select(x => new SkillGapHistoryDto
                {
                    SkillGapAnalysisHistoryId =
                        x.SkillGapAnalysisHistoryId,

                    RoadmapId = x.RoadmapId,

                    RoadmapTitle =
                        x.RoadmapTitleSnapshot,

                    CareerRoleName =
                        x.CareerRoleNameSnapshot,

                    AuthorName =
                        x.AuthorNameSnapshot,

                    MatchedSkills = x.MatchedSkills,

                    TotalSkills = x.TotalSkills,

                    MissingSkills = x.MissingSkills,

                    CreatedAt = x.CreatedAt,
                })
                .ToListAsync(cancellationToken);

            var hasMore = items.Count > request.Limit;

            if (hasMore)
            {
                items.RemoveAt(items.Count - 1);
            }

            string? nextCursor = null;

            if (hasMore && items.Count > 0)
            {
                var lastItem = items[^1];

                nextCursor = EncodeCursor(
                    lastItem.CreatedAt,
                    lastItem.SkillGapAnalysisHistoryId);
            }

            return new SkillGapHistoryPageDto
            {
                Items = items,
                NextCursor = nextCursor,
                HasMore = hasMore,
            };
        }

        public async Task<AnalyzeSkillGapResponseDto> GetHistoryDetailAsync(
                Guid userId,
                Guid skillGapAnalysisHistoryId,
                CancellationToken cancellationToken)
        {
            var history = await _dbContext.SkillGapAnalysisHistories
                .AsNoTracking()
                .Where(x =>
                    x.SkillGapAnalysisHistoryId ==
                    skillGapAnalysisHistoryId &&
                    x.UserId == userId)
                .Select(x => new
                {
                    x.SnapshotJson,
                    x.RoadmapId,
                    x.RoadmapVersionId,
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (history is null)
            {
                throw new NotFoundException(
                    "Skill gap analysis history not found.");
            }

            AnalyzeSkillGapResponseDto? response;

            try
            {
                response =
                    JsonSerializer.Deserialize<
                        AnalyzeSkillGapResponseDto>(
                        history.SnapshotJson);
            }
            catch (JsonException)
            {
                throw new ConflictException(
                    "Skill gap analysis snapshot is invalid.");
            }

            if (response is null)
            {
                throw new ConflictException(
                    "Skill gap analysis snapshot is invalid.");
            }

            response.RoadmapId = history.RoadmapId;
            response.RoadmapVersionId = history.RoadmapVersionId;

            await EnrichNavigationAsync(response, cancellationToken);

            return response;
        }

        private async Task EnrichNavigationAsync(
            AnalyzeSkillGapResponseDto response,
            CancellationToken cancellationToken)
        {
            response.RoadmapSlug = await _dbContext.Roadmaps
                .AsNoTracking()
                .Where(roadmap =>
                    roadmap.RoadmapId == response.RoadmapId &&
                    roadmap.Visibility == "public" &&
                    roadmap.RoadmapVersions.Any(version => version.Status == "published"))
                .Select(roadmap => roadmap.Slug)
                .SingleOrDefaultAsync(cancellationToken)
                ?? string.Empty;

            var skills = response.Categories
                .SelectMany(category => category.Skills)
                .ToList();

            var skillIds = skills
                .Select(skill => skill.SkillId)
                .Where(skillId => skillId != Guid.Empty)
                .Distinct()
                .ToList();

            if (skillIds.Count == 0)
            {
                return;
            }

            var slugsBySkillId = await _dbContext.Skills
                .AsNoTracking()
                .Where(skill => skillIds.Contains(skill.SkillId))
                .ToDictionaryAsync(
                    skill => skill.SkillId,
                    skill => skill.Slug,
                    cancellationToken);

            foreach (var skill in skills)
            {
                skill.SkillSlug = slugsBySkillId.GetValueOrDefault(skill.SkillId)
                    ?? string.Empty;
            }
        }

        private static string EncodeCursor(DateTime createdAt, Guid historyId)
        {
            var normalizedCreatedAt =
                createdAt.Kind == DateTimeKind.Utc
                    ? createdAt
                    : createdAt.ToUniversalTime();

            var payload = string.Create(
                CultureInfo.InvariantCulture,
                $"{normalizedCreatedAt.Ticks}:{historyId:N}");

            var bytes = Encoding.UTF8.GetBytes(payload);

            return Convert
                .ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static HistoryCursor DecodeCursor(string encodedCursor)
        {
            try
            {
                var normalized = encodedCursor
                    .Trim()
                    .Replace('-', '+')
                    .Replace('_', '/');

                var remainder = normalized.Length % 4;

                normalized += remainder switch
                {
                    0 => string.Empty,
                    2 => "==",
                    3 => "=",
                    _ => throw new FormatException(),
                };

                var bytes = Convert.FromBase64String(normalized);

                var payload = Encoding.UTF8.GetString(bytes);

                var parts = payload.Split(
                    ':',
                    2,
                    StringSplitOptions.None);

                if (parts.Length != 2)
                {
                    throw new FormatException();
                }

                if (!long.TryParse(
                        parts[0],
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out var createdAtTicks))
                {
                    throw new FormatException();
                }

                if (!Guid.TryParseExact(
                        parts[1],
                        "N",
                        out var historyId))
                {
                    throw new FormatException();
                }

                var createdAt = new DateTime(
                    createdAtTicks,
                    DateTimeKind.Utc);

                return new HistoryCursor(
                    createdAt,
                    historyId);
            }
            catch (Exception exception)
                when (exception is FormatException or ArgumentException)
            {
                throw new ArgumentException(
                    "Invalid history cursor.",
                    exception);
            }
        }

        private sealed record HistoryCursor(
            DateTime CreatedAt,
            Guid HistoryId);
    }
}
