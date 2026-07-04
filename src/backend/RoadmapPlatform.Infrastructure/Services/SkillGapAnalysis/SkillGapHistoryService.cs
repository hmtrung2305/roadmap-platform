using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.SkillGapAnalysis.Analysis;
using RoadmapPlatform.Application.DTOs.SkillGapAnalysis.History;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.SkillGapAnalysis;
using RoadmapPlatform.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace RoadmapPlatform.Infrastructure.Services.SkillGapAnalysis
{
    public class SkillGapHistoryService : ISkillGapHistoryService
    {
        private readonly ApplicationDbContext _dbContext;

        public SkillGapHistoryService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task DeleteHistoryAsync(Guid userId, Guid skillGapAnalysisHistoryId)
        {
            var history = await _dbContext.SkillGapAnalysisHistories
                .SingleOrDefaultAsync(x =>
                    x.SkillGapAnalysisHistoryId == skillGapAnalysisHistoryId &&
                    x.UserId == userId &&
                    !x.IsDeleted);

            if (history is null)
            {
                throw new NotFoundException(
                    "Skill gap analysis history not found.");
            }

            history.IsDeleted = true;

            history.DeletedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<SkillGapHistoryDto>> GetHistoryAsync(Guid userId)
        {
            return await _dbContext.SkillGapAnalysisHistories
                .AsNoTracking()
                .Where(x =>
                    x.UserId == userId &&
                    !x.IsDeleted)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new SkillGapHistoryDto
                {
                    SkillGapAnalysisHistoryId = x.SkillGapAnalysisHistoryId,

                    RoadmapId = x.RoadmapId,

                    RoadmapTitle = x.RoadmapTitleSnapshot,

                    CareerRoleName = x.CareerRoleNameSnapshot,

                    AuthorName = x.AuthorNameSnapshot,

                    MatchedSkills = x.MatchedSkills,

                    TotalSkills = x.TotalSkills,

                    MissingSkills = x.MissingSkills,

                    CreatedAt = x.CreatedAt,
                })
                .ToListAsync();
        }

        public async Task<AnalyzeSkillGapResponseDto> GetHistoryDetailAsync(Guid userId, Guid skillGapAnalysisHistoryId)
        {
            var history = await _dbContext.SkillGapAnalysisHistories
                    .AsNoTracking()
                    .Where(x =>
                        x.SkillGapAnalysisHistoryId == skillGapAnalysisHistoryId &&
                        x.UserId == userId &&
                        !x.IsDeleted)
                    .Select(x => new
                    {
                        x.SnapshotJson,
                    })
                    .SingleOrDefaultAsync();

            if (history is null)
            {
                throw new NotFoundException(
                    "Skill gap analysis history not found.");
            }

            var response = JsonSerializer.Deserialize<AnalyzeSkillGapResponseDto>(
                history.SnapshotJson);

            if (response is null)
            {
                throw new ConflictException(
                    "Skill gap analysis snapshot is invalid.");
            }

            return response;
        }
    }
}
