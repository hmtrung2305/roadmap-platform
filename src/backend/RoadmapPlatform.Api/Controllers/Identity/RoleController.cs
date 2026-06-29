using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.PermissionRole;
using RoadmapPlatform.Application.DTOs.Role;
using RoadmapPlatform.Application.Interfaces.Identity;

namespace RoadmapPlatform.Api.Controllers.Identity
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [RequirePermission(PermissionConstant.ROLE_VIEW_ANY)]
        [HttpGet]
        public async Task<IActionResult> GetAllRole()
        {
            var roles = await _roleService.GetRolesAsync();
            return Ok(new
            {
                Success = true,
                Message = "Get all role successfully",
                Data = roles
            });
        }

        [RequirePermission(PermissionConstant.ROLE_VIEW_ANY)]
        [RequirePermission(PermissionConstant.ROLE_PERMISSION_VIEW_ANY)]
        [HttpGet("{id:Guid}")]
        public async Task<IActionResult> GetRoleById(Guid id)
        {
            var role = await _roleService.GetRoleByIdAsync(id);
            return Ok(new
            {
                Success = true,
                Message = "Get role detail successfully",
                Data = role
            });
        }

        [RequirePermission(PermissionConstant.ROLE_DELETE_ANY)]
        [HttpDelete("{id:Guid}")]
        public async Task<IActionResult> DeleteRoleById(Guid id)
        {
            await _roleService.DeleteRoleAsync(id);
            return NoContent();
        }

        [RequirePermission(PermissionConstant.ROLE_CREATE_ANY)]
        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequestDto roleRequest)
        {
            var role = await _roleService.CreateRoleAsync(roleRequest);
            return CreatedAtAction(nameof(GetRoleById), new { id = role.RoleId }, role);
        }

        [RequirePermission(PermissionConstant.ROLE_UPDATE_ANY)]
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<RoleResponseDto>> UpdateRole(Guid id, [FromBody] UpdateRoleRequestDto request)
        {
            var role = await _roleService.UpdateRoleAsync(id, request);

            return Ok(new
            {
                Success = true,
                Message = "Update role detail successfully",
                Data = role
            });
        }

        [RequirePermission(PermissionConstant.ROLE_PERMISSION_ASSIGN_ANY)]
        [HttpPost("{id:guid}/permissions/{permissionId:guid}")]
        public async Task<ActionResult<RoleDetailResponseDto>> GrantPermission(Guid id, Guid permissionId)
        {
            var role = await _roleService.GrantRolePermissionAsync(id, permissionId);

            return Ok(new
            {
                Success = true,
                Message = "Permission assigned to role successfully",
                Data = role
            });
        }

        [RequirePermission(PermissionConstant.ROLE_PERMISSION_REVOKE_ANY)]
        [HttpDelete("{id:guid}/permissions/{permissionId:guid}")]
        public async Task<ActionResult<RoleDetailResponseDto>> RevokePermission(Guid id, Guid permissionId)
        {
            var role = await _roleService.RevokeRolePermissionAsync(id, permissionId);

            return Ok(new
            {
                Success = true,
                Message = "Permission revoked from role successfully",
                Data = role
            });
        }

        [RequirePermission(PermissionConstant.ROLE_PERMISSION_ASSIGN_ANY)]
        [RequirePermission(PermissionConstant.ROLE_PERMISSION_REVOKE_ANY)]
        [HttpPut("{id:guid}/permissions")]
        public async Task<ActionResult<RoleDetailResponseDto>> AssignPermissions(Guid id, [FromBody] AssignPermissionRoleRequestDto request)
        {
            var role = await _roleService.AssignRolePermissionsAsync(id, request);

            return Ok(new
            {
                Success = true,
                Message = "Update role detail successfully",
                Data = role
            });
        }
    }
}
