using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.Permissions;
using RoadmapPlatform.Application.Interfaces.Identity;

namespace RoadmapPlatform.Api.Controllers.Identity
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

        [RequirePermission(PermissionConstant.PERMISSION_VIEW_ANY)]
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

        [RequirePermission(PermissionConstant.PERMISSION_VIEW_ANY)]
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

        [RequirePermission(PermissionConstant.PERMISSION_UPDATE_ANY)]
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

        [RequirePermission(PermissionConstant.PERMISSION_DELETE_ANY)]
        [HttpDelete("{id:Guid}")]
        public async Task<IActionResult> DeletePermission(Guid id)
        {
            await _permissionService.DeletePermissionAsync(id);
            return NoContent();
        }

        [RequirePermission(PermissionConstant.PERMISSION_CREATE_ANY)]
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
