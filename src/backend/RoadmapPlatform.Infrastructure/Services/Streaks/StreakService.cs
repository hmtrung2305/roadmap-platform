using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.Streaks;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces;
using RoadmapPlatform.Application.Interfaces.Streaks;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.Streaks
{
    public class StreakService : IStreakService
    {
        private readonly ApplicationDbContext _dbContext;

        public StreakService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<StreakDto> GetStreakAsync(Guid userId)
        {
            var user = await _dbContext.Users.AsNoTracking()
                .AnyAsync(x => x.UserId == userId);
            if (!user)
            {
                throw new NotFoundException("User is not found");
            }
            var stats = await _dbContext.UserActivityStats.AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if(stats == null )
            {
                return new StreakDto
                {
                    CurrentStreak = 0,
                    LongestStreak = 0,
                    LastInteraction = null,
                    IncreasedToday = false,
                    IsCompletedStreakToday = false
                };
            }
            return MapToStreakDto(stats, increasedToday: false);
        }

        public async Task<StreakDto> TrackStreakAsync(Guid userId)
        {
            var userExists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(x => x.UserId == userId);

            if (!userExists)
            {
                throw new NotFoundException("User was not found");
            }

            var stats = await _dbContext.UserActivityStats
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (stats == null)
            {
                stats = new UserActivityStat
                {
                    UserId = userId,
                    CurrentStreak = 0,
                    LongestStreak = 0,
                    LastInteraction = null
                };

                _dbContext.UserActivityStats.Add(stats);
            }

            var now = DateTime.UtcNow;
            var today = now.Date;
            var yesterday = today.AddDays(-1);
            var increasedToday = false;

            if (!stats.LastInteraction.HasValue)
            {
                stats.CurrentStreak = 1;
                stats.LongestStreak = Math.Max(stats.LongestStreak, stats.CurrentStreak);
                stats.LastInteraction = now;
                increasedToday = true;
            }
            else
            {
                var lastDate = stats.LastInteraction.Value.Date;

                if (lastDate == today)
                {
                    increasedToday = false;
                }
                else if (lastDate == yesterday)
                {
                    stats.CurrentStreak += 1;
                    stats.LongestStreak = Math.Max(stats.LongestStreak, stats.CurrentStreak);
                    stats.LastInteraction = now;
                    increasedToday = true;
                }
                else
                {
                    stats.CurrentStreak = 1;
                    stats.LongestStreak = Math.Max(stats.LongestStreak, stats.CurrentStreak);
                    stats.LastInteraction = now;
                    increasedToday = true;
                }
            }

            await _dbContext.SaveChangesAsync();

            return MapToStreakDto(stats, increasedToday);
        }
        private static StreakDto MapToStreakDto(UserActivityStat stats, bool increasedToday)
        {
            var today = DateTime.UtcNow.Date;
            return new StreakDto
            {
                CurrentStreak = stats.CurrentStreak,
                LongestStreak = stats.LongestStreak,
                LastInteraction = stats.LastInteraction,
                IncreasedToday = increasedToday,
                IsCompletedStreakToday = stats.LastInteraction.HasValue
                                            && stats.LastInteraction.Value.Date == today
            };
        }
    }
}
