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

        public decimal ReadinessPercent { get; set; }

        public decimal SkillCoveragePercent { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
