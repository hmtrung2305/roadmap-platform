using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis
{
    public sealed class CareerRoleOptionDto
    {
        public Guid CareerRoleId { get; set; }

        public string Name { get; set; }
            = string.Empty;

        public string Slug { get; set; }
            = string.Empty;
    }
}
