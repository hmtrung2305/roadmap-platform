using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.PermissionRole;
using RoadmapPlatform.Application.DTOs.Role;
using RoadmapPlatform.Application.Interfaces.Identity;

namespace RoadmapPlatform.Api.Controllers.Identity
{
    /// <summary>
    /// Provides role management endpoints for identity and authorization administration.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _roleService;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleController"/> class.
        /// </summary>
        /// <param name="roleService">The role service used to manage roles and role permissions.</param>
        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        /// <summary>
        /// Gets all roles.
        /// </summary>
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

        /// <summary>
        /// Gets role details by role identifier.
        /// </summary>
        /// <param name="id">The role identifier.</param>
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

        /// <summary>
        /// Deletes a role by role identifier.
        /// </summary>
        /// <param name="id">The role identifier.</param>
        [RequirePermission(PermissionConstant.ROLE_DELETE_ANY)]
        [HttpDelete("{id:Guid}")]
        public async Task<IActionResult> DeleteRoleById(Guid id)
        {
            await _roleService.DeleteRoleAsync(id);
            return NoContent();
        }

        /// <summary>
        /// Creates a new role.
        /// </summary>
        /// <param name="roleRequest">The role creation request.</param>
        [RequirePermission(PermissionConstant.ROLE_CREATE_ANY)]
        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequestDto roleRequest)
        {
            var role = await _roleService.CreateRoleAsync(roleRequest);
            return CreatedAtAction(nameof(GetRoleById), new { id = role.RoleId }, role);
        }

        /// <summary>
        /// Updates role details.
        /// </summary>
        /// <param name="id">The role identifier.</param>
        /// <param name="request">The role update request.</param>
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

        /// <summary>
        /// Grants a permission to a role.
        /// </summary>
        /// <param name="id">The role identifier.</param>
        /// <param name="permissionId">The permission identifier.</param>
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

        /// <summary>
        /// Revokes a permission from a role.
        /// </summary>
        /// <param name="id">The role identifier.</param>
        /// <param name="permissionId">The permission identifier.</param>
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

        /// <summary>
        /// Replaces the permission set assigned to a role.
        /// </summary>
        /// <param name="id">The role identifier.</param>
        /// <param name="request">The permission assignment request.</param>
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
