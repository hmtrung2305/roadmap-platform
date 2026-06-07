using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Application.Interfaces.Portfolio;

namespace RoadmapPlatform.Api.Controllers.Users
{
    [Route("api/portfolios")]
    [ApiController]
    public class PortfolioController : ControllerBase
    {
        private readonly IPortfolioService _portfolioService;

        public PortfolioController(IPortfolioService portfolioService)
        {
            _portfolioService = portfolioService;
        }

        [HttpGet("{username}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPortfolioByUsername(string username)
        {
            var portfolio = await _portfolioService.GetPortfolioByUsernameAsync(username);

            return Ok(portfolio);
        }

    }
}
