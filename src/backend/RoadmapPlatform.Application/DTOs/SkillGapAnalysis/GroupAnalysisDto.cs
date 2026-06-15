using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis
{
    public sealed class GroupAnalysisDto
    {
        public Guid SkillGroupId { get; set; }

        public string GroupName { get; set; }
            = default!;

        public int Priority { get; set; }

        public LearningPriority LearningPriority { get; set; }

        public int MatchedSkillCount { get; set; }

        public int TotalSkillCount { get; set; }

        public string CompletionRule { get; set; }
            = default!;

        public int? RequiredSkillCount { get; set; }

        public bool IsCompleted { get; set; }

        public List<string> MatchedSkills { get; set; }
            = [];

        public List<string> SuggestedSkills { get; set; }
            = [];
    }
}
