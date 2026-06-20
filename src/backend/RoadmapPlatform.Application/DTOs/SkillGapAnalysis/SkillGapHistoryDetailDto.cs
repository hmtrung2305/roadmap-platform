using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis
{
    public sealed class SkillGapHistoryDetailDto
    {
        public Guid HistoryId { get; set; }

        public DateTime CreatedAt { get; set; }

        public AnalyzeSkillGapResponseDto Result { get; set; }
            = null!;
    }
}
