using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis.CategoryConfig
{
    public class UpdateCategoryDisplayOrderDto
    {
        public string CategoryName { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }
    }
}
