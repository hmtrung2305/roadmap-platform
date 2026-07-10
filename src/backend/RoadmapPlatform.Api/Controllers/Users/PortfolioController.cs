using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Application.Interfaces.Portfolio;

namespace RoadmapPlatform.Api.Controllers.Users
{
    /// <summary>
    /// Provides public portfolio endpoints.
    /// </summary>
    [Route("api/portfolios")]
    [ApiController]
    public class PortfolioController : ControllerBase
    {
        private readonly IPortfolioService _portfolioService;

        /// <summary>
        /// Initializes a new instance of the <see cref="PortfolioController"/> class.
        /// </summary>
        /// <param name="portfolioService">The portfolio service used to retrieve portfolio data.</param>
        public PortfolioController(IPortfolioService portfolioService)
        {
            _portfolioService = portfolioService;
        }

        /// <summary>
        /// Gets a public portfolio by username.
        /// </summary>
        /// <param name="username">The username of the portfolio owner.</param>
        /// <returns>The public portfolio associated with the specified username.</returns>
        [HttpGet("{username}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPortfolioByUsername(string username)
        {
            var portfolio = await _portfolioService.GetPortfolioByUsernameAsync(username);

            return Ok(portfolio);
        }
    }
}