using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.Permissions;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces;
using RoadmapPlatform.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Infrastructure.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly ApplicationDbContext _dbContext;

        public PermissionService (ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<PermissionResponseDto> CreatePermissionAsync(CreatePermissionRequestDto createPermissionRequest)
        {
            throw new NotImplementedException();
        }

        public Task DeletePermissionAsync(Guid permissionId)
        {
            throw new NotImplementedException();
        }

        public Task<PermissionResponseDto> GetPermissionByIdAsync(Guid permissionId)
        {
            throw new NotImplementedException();
        }

        public async Task<List<PermissionResponseDto>> GetPermissionsAsync()
        {
            var permissions = await _dbContext.Permissions.AsNoTracking().ToListAsync();
            if (permissions == null) throw new NotFoundException("Not Found Permisisons");

            var permissionResponseDto = permissions.Select(p => new PermissionResponseDto
            {
                PermissionId = p.PermissionId,
                PermissionName = p.PermissionName,
            }).ToList();

            return permissionResponseDto;

        }

        public Task<PermissionResponseDto> UpdatePermissionAsync(Guid permissionId, UpdatePermissionRequestDto updatePermissionRequest)
        {
            throw new NotImplementedException();
        }
    }
}
