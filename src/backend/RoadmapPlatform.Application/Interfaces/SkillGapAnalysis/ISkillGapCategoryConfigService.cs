using RoadmapPlatform.Application.DTOs.SkillGapAnalysis.CategoryConfig;
using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.Interfaces.SkillGapAnalysis
{
    public interface ISkillGapCategoryConfigService
    {
        Task GenerateCategoryConfigurationAsync(Guid roadmapId);

        Task<CategoryConfigurationResponseDto> GetCategoryConfigurationAsync(Guid actorUserId, Guid roadmapId);

        Task UpdateCategoryDisplayOrderAsync(Guid actorUserId, Guid roadmapId, List<UpdateCategoryDisplayOrderDto> request);

        Task<List<PublishedRoadmapOptionDto>> GetMyPublishedRoadmapsAsync(Guid userId);
    }
}
