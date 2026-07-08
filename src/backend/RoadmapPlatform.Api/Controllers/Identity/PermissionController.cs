using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.Permissions;
using RoadmapPlatform.Application.Interfaces.Identity;

namespace RoadmapPlatform.Api.Controllers.Identity
{
    /// <summary>
    /// Provides permission management endpoints for identity and authorization administration.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PermissionController : ControllerBase
    {
        private readonly IPermissionService _permissionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="PermissionController"/> class.
        /// </summary>
        /// <param name="permissionService">The permission service used to manage permissions.</param>
        public PermissionController(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        /// <summary>
        /// Gets all permissions.
        /// </summary>
        [RequirePermission(PermissionConstant.PERMISSION_VIEW_ANY)]
        [HttpGet]
        public async Task<IActionResult> GetAllPermissions()
        {
            var permissions = await _permissionService.GetPermissionsAsync();
            return Ok(new
            {
                Success = true,
                Message = "Permissions retrieved successfully",
                Data = permissions
            });
        }

        /// <summary>
        /// Gets permission details by permission identifier.
        /// </summary>
        /// <param name="id">The permission identifier.</param>
        [RequirePermission(PermissionConstant.PERMISSION_VIEW_ANY)]
        [HttpGet("{id:Guid}")]
        public async Task<IActionResult> GetPermissionById(Guid id)
        {
            var permission = await _permissionService.GetPermissionByIdAsync(id);
            return Ok(new
            {
                Success = true,
                Message = "Permission detail retrieved successfully",
                Data = permission
            });
        }

        /// <summary>
        /// Updates permission details.
        /// </summary>
        /// <param name="id">The permission identifier.</param>
        /// <param name="permissionRequest">The permission update request.</param>
        [RequirePermission(PermissionConstant.PERMISSION_UPDATE_ANY)]
        [HttpPut("{id:Guid}")]
        public async Task<IActionResult> UpdatePermission(Guid id, [FromBody] UpdatePermissionRequestDto permissionRequest)
        {
            var permission = await _permissionService.UpdatePermissionAsync(id, permissionRequest);
            return Ok(new
            {
                Success = true,
                Message = "Permission updated successfully",
                Data = permission
            });
        }

        /// <summary>
        /// Deletes a permission by permission identifier.
        /// </summary>
        /// <param name="id">The permission identifier.</param>
        [RequirePermission(PermissionConstant.PERMISSION_DELETE_ANY)]
        [HttpDelete("{id:Guid}")]
        public async Task<IActionResult> DeletePermission(Guid id)
        {
            await _permissionService.DeletePermissionAsync(id);
            return NoContent();
        }

        /// <summary>
        /// Creates a new permission.
        /// </summary>
        /// <param name="request">The permission creation request.</param>
        [RequirePermission(PermissionConstant.PERMISSION_CREATE_ANY)]
        [HttpPost]
        public async Task<IActionResult> CreatePermission([FromBody] CreatePermissionRequestDto request)
        {
            var permission = await _permissionService.CreatePermissionAsync(request);

            return CreatedAtAction(
                nameof(GetPermissionById),
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
