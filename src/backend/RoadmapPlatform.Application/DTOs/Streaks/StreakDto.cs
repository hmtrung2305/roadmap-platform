using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.Streaks
{
    public class StreakDto
    {
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public DateTime? LastInteraction { get; set; }
        public bool IncreasedToday { get; set; }
        public bool IsCompletedStreakToday { get; set; }
    }
}
