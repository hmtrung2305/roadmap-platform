using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis
{
    public sealed class SkillGapReportResponseDto
    {
        public string CareerRoleName { get; set; }
            = default!;

        public decimal ReadinessPercent { get; set; }

        public string SkillLevel { get; set; }
            = default!;

        public List<string> Strengths { get; set; }
            = [];

        public List<string> SkillGaps { get; set; }
            = [];

        public List<string> UrgentLearningPriorities
        { get; set; } = [];

        public List<string> RecommendedLearningPath
        { get; set; } = [];
    }
}
