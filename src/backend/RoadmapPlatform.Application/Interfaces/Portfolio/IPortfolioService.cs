using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RoadmapPlatform.Application.DTOs.Portfolio;

namespace RoadmapPlatform.Application.Interfaces.Portfolio
{
    public interface IPortfolioService
    {
        Task<PortfolioResponseDto> GetMyPortfolioAsync(Guid userId);
        Task<PortfolioResponseDto> GetPortfolioByUsernameAsync(string username);
        Task<PortfolioResponseDto> UpdatePortfolioRepositoriesAsync(Guid userId, UpdatePortfolioRepositoriesRequestDto request);
    }
}