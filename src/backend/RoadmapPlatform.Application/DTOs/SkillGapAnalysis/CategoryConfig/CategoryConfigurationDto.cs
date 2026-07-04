using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis.CategoryConfig
{
    public class CategoryConfigurationDto
    {
        public string CategoryName { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }

        public int TotalSkills { get; set; }

        public List<CategorySkillDto> Skills { get; set; } = [];
    }
}
