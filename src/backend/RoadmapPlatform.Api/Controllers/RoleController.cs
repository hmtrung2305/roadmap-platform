using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Application.DTOs.PermissionRole;
using RoadmapPlatform.Application.DTOs.Role;
using RoadmapPlatform.Application.Interfaces;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services;
using System.Data;

namespace RoadmapPlatform.Api.Controllers
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

        [HttpDelete("{id:Guid}")]
        public async Task<IActionResult> DeleteRoleById(Guid id)
        {
            await _roleService.DeleteRoleAsync(id);
            return NoContent();
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequestDto roleRequest)
        {
            var role = await _roleService.CreateRoleAsync(roleRequest);
            return CreatedAtAction(nameof(GetRoleById), new { id = role.RoleId }, role);
        }

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
