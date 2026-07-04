using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis.History
{
    public class SkillGapHistoryDto
    {
        public Guid SkillGapAnalysisHistoryId { get; set; }

        public Guid RoadmapId { get; set; }

        public string RoadmapTitle { get; set; } = string.Empty;

        public string CareerRoleName { get; set; } = string.Empty;

        public string AuthorName { get; set; } = string.Empty;

        public int MatchedSkills { get; set; }

        public int TotalSkills { get; set; }

        public int MissingSkills { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
