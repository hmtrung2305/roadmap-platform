namespace RoadmapPlatform.Application.DTOs.Portfolio
{
    public class UpdatePortfolioRepositoriesRequestDto
    {
        public List<Guid> RepositoryIds { get; set; } = new();
    }
}
