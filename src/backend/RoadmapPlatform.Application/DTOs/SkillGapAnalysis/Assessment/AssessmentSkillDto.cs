using System;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis.Assessment
{
    public class AssessmentSkillDto
    {
        public Guid SkillId { get; set; }

        public string SkillName { get; set; } = string.Empty;

        public bool IsSuggestedFromCompletedNodes { get; set; }

        public int CompletedNodeCount { get; set; }
    }
}