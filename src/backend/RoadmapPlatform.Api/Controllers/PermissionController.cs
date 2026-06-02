using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Application.Interfaces;

namespace RoadmapPlatform.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PermissionController : ControllerBase
    {
        private readonly IPermissionService _permissionService;
        public PermissionController(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPermissions()
        {
            var permissions = await _permissionService.GetPermissionsAsync();
            return Ok(permissions);
        }

    }
}
