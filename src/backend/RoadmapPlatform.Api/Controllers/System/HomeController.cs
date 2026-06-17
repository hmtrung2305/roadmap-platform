using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Api.Responses;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Infrastructure.Data;

namespace RoadmapPlatform.Api.Controllers.System
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : ControllerBase
    {

        private readonly ApplicationDbContext _context;
        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("check-connection")]
        [RequirePermission(PermissionConstant.SYSTEM_HEALTH_VIEW_ANY)]
        public async Task<IActionResult> CheckConnection(CancellationToken cancellationToken)
        {
            try
            {
                // Kiểm tra nhanh xem EF Core có mở được kết nối tới DB không
                bool canConnect = await _context.Database.CanConnectAsync(cancellationToken);

                if (canConnect)
                {
                    return Ok(new { status = "Success", message = "Kết nối Database thành công!" });
                }

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiErrorResponseFactory.Create(
                        HttpContext,
                        StatusCodes.Status500InternalServerError,
                        "DATABASE_CONNECTION_FAILED",
                        "Không thể kết nối đến Database."));
            }
            catch
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiErrorResponseFactory.Create(
                        HttpContext,
                        StatusCodes.Status500InternalServerError,
                        "DATABASE_CONNECTION_FAILED",
                        "Không thể kết nối đến Database."));
            }
        }
    }
}
