using RoadmapPlatform.Application.DTOs.PermissionRole;
using RoadmapPlatform.Application.DTOs.Role;
using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.Interfaces.Identity
{
    public interface IRoleService
    {
        Task<List<RoleResponseDto>> GetRolesAsync();
        Task<RoleDetailResponseDto> GetRoleByIdAsync(Guid roleId);
        Task<RoleResponseDto> CreateRoleAsync(CreateRoleRequestDto roleRequest);
        Task<RoleResponseDto> UpdateRoleAsync(Guid roleId, UpdateRoleRequestDto roleRequest);
        Task DeleteRoleAsync(Guid roleId);
        Task<RoleDetailResponseDto> AssignRolePermissionsAsync(Guid roleId, AssignPermissionRoleRequestDto request);
    }
}
