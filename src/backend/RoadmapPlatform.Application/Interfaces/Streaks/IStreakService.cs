using RoadmapPlatform.Application.DTOs.Streaks;
using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.Interfaces.Streaks
{
    public interface IStreakService
    {
        Task<StreakDto> GetStreakAsync(Guid userId);

        Task<StreakDto> TrackStreakAsync(Guid userId);
    }
}
