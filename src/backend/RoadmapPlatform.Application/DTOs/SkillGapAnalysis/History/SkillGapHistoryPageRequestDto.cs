using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis.History
{
    public sealed class SkillGapHistoryPageRequestDto
    {
        [Range(
            1,
            50,
            ErrorMessage = "Limit must be between 1 and 50.")]
        public int Limit { get; set; } = 20;

        public string? Cursor { get; set; }
    }
}
