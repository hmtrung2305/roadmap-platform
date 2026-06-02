using RoadmapPlatform.Application.DTOs.Permissions;
using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.Interfaces
{
    public interface IPermissionService
    {
        Task<List<PermissionResponseDto>> GetPermissionsAsync();
        Task<PermissionResponseDto> GetPermissionByIdAsync(Guid permissionId);
        Task<PermissionResponseDto> CreatePermissionAsync(CreatePermissionRequestDto createPermissionRequest);
        Task<PermissionResponseDto> UpdatePermissionAsync(Guid permissionId, UpdatePermissionRequestDto updatePermissionRequest);
        Task DeletePermissionAsync(Guid permissionId);
    }
}
