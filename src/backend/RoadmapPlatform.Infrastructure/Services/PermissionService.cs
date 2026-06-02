using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.Permissions;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Infrastructure.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly ApplicationDbContext _dbContext;

        public PermissionService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<PermissionResponseDto> CreatePermissionAsync(CreatePermissionRequestDto createPermissionRequest)
        {
            var existedPermission = await _dbContext.Permissions
                                    .AnyAsync(p => p.PermissionName == createPermissionRequest.PermissionName);

            if (existedPermission) throw new ConflictException("Permission name already exists");

            var permission = new Permission
            {
                PermissionName = createPermissionRequest.PermissionName
            };

            _dbContext.Permissions.Add(permission);

            await _dbContext.SaveChangesAsync();

            return new PermissionResponseDto
            {
                PermissionId = permission.PermissionId,
                PermissionName = permission.PermissionName
            };
        }

        public async Task DeletePermissionAsync(Guid permissionId)
        {
            var permission = await _dbContext.Permissions.FindAsync(permissionId);

            if (permission == null) throw new NotFoundException("Permission not found");

            _dbContext.Permissions.Remove(permission);

            await _dbContext.SaveChangesAsync();
        }

        public async Task<PermissionResponseDto> GetPermissionByIdAsync(Guid permissionId)
        {
            var permission = await _dbContext.Permissions.FindAsync(permissionId);
            if (permission == null) throw new NotFoundException("Not Found Permissions");

            var permissionResponseDto = new PermissionResponseDto
            {
                PermissionId = permission.PermissionId,
                PermissionName = permission.PermissionName
            };
            return permissionResponseDto;

        }

        public async Task<List<PermissionResponseDto>> GetPermissionsAsync()
        {
            var permissions = await _dbContext.Permissions.AsNoTracking().ToListAsync();
            //if (permissions == null) throw new NotFoundException("Not Found Permissions");

            var permissionResponseDto = permissions.Select(p => new PermissionResponseDto
            {
                PermissionId = p.PermissionId,
                PermissionName = p.PermissionName,
            }).ToList();

            return permissionResponseDto;

        }

        public async Task<PermissionResponseDto> UpdatePermissionAsync(Guid permissionId, UpdatePermissionRequestDto updatePermissionRequest)
        {
            var permission = await _dbContext.Permissions.FindAsync(permissionId);
            if (permission == null) throw new NotFoundException("Not Found Permissions");


            var existedPermission = await _dbContext.Permissions.AnyAsync(p =>
                                    p.PermissionName == updatePermissionRequest.PermissionName &&
                                    p.PermissionId != permissionId);

            if (existedPermission) throw new ConflictException("Permission name already exists");

            permission.PermissionName = updatePermissionRequest.PermissionName;

            await _dbContext.SaveChangesAsync();

            return new PermissionResponseDto
            {
                PermissionId = permission.PermissionId,
                PermissionName = permission.PermissionName
            };
        }
    }
}
