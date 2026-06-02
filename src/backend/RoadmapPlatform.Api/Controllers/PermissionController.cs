using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Application.DTOs.Permissions;
using RoadmapPlatform.Application.Interfaces;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services;

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
            return Ok(new
            {
                Success = true,
                Message = "Get all permission successfully",
                Data = permissions
            });
        }

        [HttpGet("{id:Guid}")]
        public async Task<IActionResult> GetPermissionById(Guid id)
        {
            var permission = await _permissionService.GetPermissionByIdAsync(id);
            return Ok(new
            {
                Success = true,
                Message = "Get permission detail successfully",
                Data = permission
            });
        }

        [HttpPut("{id:Guid}")]
        public async Task<IActionResult> UpdatePermission(Guid id, [FromBody] UpdatePermissionRequestDto permissionRequest)
        {
            var permission = await _permissionService.UpdatePermissionAsync(id, permissionRequest);
            return Ok(new
            {
                Success = true,
                Message = "permission updated successfully",
                Data = permission
            });
        }

        [HttpDelete("{id:Guid}")]
        public async Task<IActionResult> DeletePermission(Guid id)
        {
            await _permissionService.DeletePermissionAsync(id);
            return NoContent();
        }

        [HttpPost]
        public async Task<IActionResult> CreatePermission([FromBody] CreatePermissionRequestDto request)
        {
            var permission = await _permissionService.CreatePermissionAsync(request);

            return CreatedAtAction(nameof(GetPermissionById),
                new
                {
                    id = permission.PermissionId
                },
                new
                {
                    Success = true,
                    Message = "Permission created successfully",
                    Data = permission
                });
        }
    }
}
