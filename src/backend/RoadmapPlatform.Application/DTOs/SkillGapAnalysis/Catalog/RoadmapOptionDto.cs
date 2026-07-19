using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis.Catalog
{
    public class RoadmapOptionDto
    {
        public Guid RoadmapId { get; set; }

        public Guid PublishedRoadmapVersionId { get; set; }

        public string Slug { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string AuthorName { get; set; } = string.Empty;

        public int VersionNumber { get; set; }

        public DateTime? PublishedAt { get; set; }

        public int TotalSkills { get; set; }
    }
}
