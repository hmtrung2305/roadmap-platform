using RoadmapPlatform.Application.DTOs.GitHub;
using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.Interfaces.GitHub
{
    public interface IGitHubRepositoryService
    {
        Task<List<GitHubRepositoryResponseDto>> SyncPublicRepositoriesAsync(Guid userId);
        Task<List<GitHubRepositoryResponseDto>> GetSavedRepositoriesAsync(Guid userId);
    }
}
