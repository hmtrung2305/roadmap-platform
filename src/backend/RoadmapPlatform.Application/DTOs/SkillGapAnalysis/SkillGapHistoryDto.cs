using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis
{
    public sealed class SkillGapHistoryDto
    {
        public Guid HistoryId { get; set; }

        public string CareerRoleName { get; set; }
            = string.Empty;

        public string LevelName { get; set; }
            = string.Empty;

        public int RoadmapVersionNumber { get; set; }

        public string RoadmapVersionTitle { get; set; } = string.Empty;

        public int MatchedSkills { get; set; }

        public int TotalSkills { get; set; }

        public int MissingSkills { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
