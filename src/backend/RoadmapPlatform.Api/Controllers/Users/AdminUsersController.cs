using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Api.Extensions;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.Users;
using RoadmapPlatform.Application.Interfaces.Users;

namespace RoadmapPlatform.Api.Controllers.Users;

[ApiController]
[Route("api/admin/users")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _adminUserService;

    public AdminUsersController(IAdminUserService adminUserService)
    {
        _adminUserService = adminUserService;
    }

    [HttpGet]
    [RequirePermission(PermissionConstant.USER_VIEW_ANY)]
    [RequirePermission(PermissionConstant.USER_ROLE_VIEW_ANY)]
    public async Task<ActionResult<List<AdminUserResponseDto>>> GetUsers([FromQuery] string? search)
    {
        var users = await _adminUserService.GetUsersAsync(search);

        return Ok(new
        {
            Success = true,
            Message = "Get users successfully",
            Data = users
        });
    }

    [HttpGet("{userId:guid}")]
    [RequirePermission(PermissionConstant.USER_VIEW_ANY)]
    [RequirePermission(PermissionConstant.USER_ROLE_VIEW_ANY)]
    public async Task<ActionResult<AdminUserResponseDto>> GetUserById(Guid userId)
    {
        var user = await _adminUserService.GetUserByIdAsync(userId);

        return Ok(new
        {
            Success = true,
            Message = "Get user detail successfully",
            Data = user
        });
    }

    [HttpPost("{userId:guid}/roles/{roleId:guid}")]
    [RequirePermission(PermissionConstant.USER_ROLE_ASSIGN_ANY)]
    public async Task<ActionResult<AdminUserResponseDto>> AssignRole(Guid userId, Guid roleId)
    {
        var user = await _adminUserService.AssignUserRoleAsync(userId, roleId);

        return Ok(new
        {
            Success = true,
            Message = "Role assigned to user successfully",
            Data = user
        });
    }

    [HttpDelete("{userId:guid}/roles/{roleId:guid}")]
    [RequirePermission(PermissionConstant.USER_ROLE_REVOKE_ANY)]
    public async Task<ActionResult<AdminUserResponseDto>> RevokeRole(Guid userId, Guid roleId)
    {
        var user = await _adminUserService.RevokeUserRoleAsync(userId, roleId, User.GetUserId());

        return Ok(new
        {
            Success = true,
            Message = "Role revoked from user successfully",
            Data = user
        });
    }
}
