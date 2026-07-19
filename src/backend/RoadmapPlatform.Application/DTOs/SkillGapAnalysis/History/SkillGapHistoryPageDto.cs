using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis.History
{
    public sealed class SkillGapHistoryPageDto
    {
        public List<SkillGapHistoryDto> Items { get; set; } = [];

        public string? NextCursor { get; set; }

        public bool HasMore { get; set; }
    }
}
