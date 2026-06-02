using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Infrastructure.Data;

namespace RoadmapPlatform.Api.Controllers
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
        public async Task<IActionResult> CheckConnection()
        {
            try
            {
                // Kiểm tra nhanh xem EF Core có mở được kết nối tới DB không
                bool canConnect = await _context.Database.CanConnectAsync();

                if (canConnect)
                {
                    return Ok(new { status = "Success", message = "Kết nối Database thành công!" });
                }

                return StatusCode(500, new { status = "Error", message = "Không thể kết nối đến Database." });  
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "Exception", message = ex.Message });
            }
        }
    }
}
