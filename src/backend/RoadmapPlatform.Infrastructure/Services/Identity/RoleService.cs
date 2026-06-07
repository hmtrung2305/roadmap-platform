using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.PermissionRole;
using RoadmapPlatform.Application.DTOs.Permissions;
using RoadmapPlatform.Application.DTOs.Role;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.Identity;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Security;
using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Infrastructure.Services.Identity
{
    public class RoleService : IRoleService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IPermissionCache _permissionCache;

        public RoleService(ApplicationDbContext dbContext, IPermissionCache permissionCache)
        {
            _dbContext = dbContext;
            _permissionCache = permissionCache;
        }

        public async Task<RoleResponseDto> CreateRoleAsync(CreateRoleRequestDto roleRequest)
        {
            var exists = await _dbContext.Roles.AnyAsync(r => r.RoleName.ToLower() == roleRequest.RoleName.ToLower());

            if (exists) throw new ConflictException("Role name already exists");

            var role = new Role
            {
                RoleName = roleRequest.RoleName.ToLower()
            };

            _dbContext.Roles.Add(role);

            await _dbContext.SaveChangesAsync();

            return new RoleResponseDto
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName
            };
        }

        public async Task DeleteRoleAsync(Guid roleId)
        {
            var role = await _dbContext.Roles.FindAsync(roleId);

            if (role == null) throw new NotFoundException("Not found role");

            _dbContext.Roles.Remove(role);

            await _dbContext.SaveChangesAsync();
            _permissionCache.Invalidate();
        }

        public async Task<RoleDetailResponseDto> GetRoleByIdAsync(Guid roleId)
        {
            var role = await _dbContext.Roles
                .Include(r => r.PermissionRoles)
                .ThenInclude(pr => pr.Permission)
                .FirstOrDefaultAsync(r => r.RoleId == roleId);
            if (role == null) throw new NotFoundException("Not found role");

            var roleResponse = new RoleDetailResponseDto
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName,
                Permissions = role.PermissionRoles
                .Select(pr => new PermissionResponseDto
                {
                    PermissionId = pr.Permission.PermissionId,
                    PermissionName = pr.Permission.PermissionName
                }).ToList()
            };
            return roleResponse;
        }

        public async Task<List<RoleResponseDto>> GetRolesAsync()
        {
            var roles = await _dbContext.Roles.AsNoTracking().ToListAsync();

            var roleResponse = roles.Select(r => new RoleResponseDto
            {
                RoleId = r.RoleId,
                RoleName = r.RoleName
            }).ToList();

            return roleResponse;
        }

        public async Task<RoleResponseDto> UpdateRoleAsync(Guid roleId, UpdateRoleRequestDto roleRequest)
        {
            var role = await _dbContext.Roles
                .FirstOrDefaultAsync(r => r.RoleId == roleId);

            if (role == null) throw new NotFoundException("Not found role");

            var roleNameExists = await _dbContext.Roles
                .AnyAsync(r =>
                    r.RoleId != roleId &&
                    r.RoleName.ToLower() == roleRequest.RoleName.ToLower());

            if (roleNameExists)
                throw new ConflictException("Role name already exists");

            role.RoleName = roleRequest.RoleName.ToLower();

            await _dbContext.SaveChangesAsync();
            _permissionCache.Invalidate();

            return new RoleResponseDto
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName
            };
        }

        public async Task<RoleDetailResponseDto> AssignRolePermissionsAsync(Guid roleId, AssignPermissionRoleRequestDto permissionRoleRequest)
        {
            var role = await _dbContext.Roles
                .Include(r => r.PermissionRoles)
                    .ThenInclude(pr => pr.Permission)
                .FirstOrDefaultAsync(r => r.RoleId == roleId);

            if (role == null)
                throw new NotFoundException("Not found role");

            // Remove duplicate
            var requestPermissionIds = permissionRoleRequest.PermissionIds
                .Distinct()
                .ToHashSet();

            // Permission hiện tại
            var currentPermissionIds = role.PermissionRoles
                .Select(pr => pr.PermissionId)
                .ToHashSet();

            // =========================
            // ADD
            // =========================

            var addPermissionIds = requestPermissionIds
                .Except(currentPermissionIds)
                .ToList();

            if (addPermissionIds.Any())
            {
                var addPermissions = addPermissionIds
                    .Select(permissionId => new PermissionRole
                    {
                        RoleId = roleId,
                        PermissionId = permissionId
                    });

                await _dbContext.PermissionRoles
                    .AddRangeAsync(addPermissions);
            }

            // =========================
            // REMOVE
            // =========================

            var removePermissionIds = currentPermissionIds
                .Except(requestPermissionIds)
                .ToList();

            if (removePermissionIds.Any())
            {
                var removePermissions = role.PermissionRoles
                    .Where(pr => removePermissionIds.Contains(pr.PermissionId))
                    .ToList();

                _dbContext.PermissionRoles
                    .RemoveRange(removePermissions);
            }

            await _dbContext.SaveChangesAsync();
            _permissionCache.Invalidate();

            return await GetRoleByIdAsync(roleId);
        }
    }
}
