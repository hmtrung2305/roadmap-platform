using RoadmapPlatform.Application.DTOs.SkillGapAnalysis.Catalog;
using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.Interfaces.SkillGapAnalysis
{
    public interface ISkillGapCatalogService
    {
        Task<List<CareerRoleOptionDto>> GetCareerRolesAsync();

        Task<List<RoadmapOptionDto>> GetPublishedRoadmapsAsync(string careerRoleSlug);
    }
}
