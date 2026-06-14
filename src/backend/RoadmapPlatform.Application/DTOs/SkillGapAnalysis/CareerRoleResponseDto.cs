using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis
{
    public sealed class CareerRoleResponseDto
    {
        public Guid CareerRoleId { get; set; }

        public string Name { get; set; } = default!;

        public string Slug { get; set; } = default!;
    }
}
