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
            var roleName = NormalizeRoleName(roleRequest.RoleName);
            var exists = await _dbContext.Roles.AnyAsync(r => r.RoleName.ToLower() == roleName);

            if (exists) throw new ConflictException("Role name already exists");

            var role = new Role
            {
                RoleName = roleName
            };

            _dbContext.Roles.Add(role);

            await _dbContext.SaveChangesAsync();
            _permissionCache.Invalidate();

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
            var role = await GetRoleWithPermissionsAsync(roleId);
            if (role == null) throw new NotFoundException("Not found role");

            return MapRoleDetail(role);
        }

        public async Task<List<RoleResponseDto>> GetRolesAsync()
        {
            var roles = await _dbContext.Roles
                .AsNoTracking()
                .OrderBy(r => r.RoleName)
                .ToListAsync();

            var roleResponse = roles.Select(r => new RoleResponseDto
            {
                RoleId = r.RoleId,
                RoleName = r.RoleName
            }).ToList();

            return roleResponse;
        }

        public async Task<RoleResponseDto> UpdateRoleAsync(Guid roleId, UpdateRoleRequestDto roleRequest)
        {
            var roleName = NormalizeRoleName(roleRequest.RoleName);
            var role = await _dbContext.Roles
                .FirstOrDefaultAsync(r => r.RoleId == roleId);

            if (role == null) throw new NotFoundException("Not found role");

            var roleNameExists = await _dbContext.Roles
                .AnyAsync(r =>
                    r.RoleId != roleId &&
                    r.RoleName.ToLower() == roleName);

            if (roleNameExists)
                throw new ConflictException("Role name already exists");

            role.RoleName = roleName;

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
            var role = await GetRoleWithPermissionsAsync(roleId);

            if (role == null)
                throw new NotFoundException("Not found role");

            // Remove duplicate
            var requestPermissionIds = (permissionRoleRequest.PermissionIds ?? new List<Guid>())
                .Distinct()
                .ToHashSet();

            await EnsurePermissionsExistAsync(requestPermissionIds);

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

        public async Task<RoleDetailResponseDto> GrantRolePermissionAsync(Guid roleId, Guid permissionId)
        {
            var role = await GetRoleWithPermissionsAsync(roleId);

            if (role == null)
                throw new NotFoundException("Not found role");

            await EnsurePermissionExistsAsync(permissionId);

            var isAssigned = role.PermissionRoles.Any(pr => pr.PermissionId == permissionId);
            if (!isAssigned)
            {
                await _dbContext.PermissionRoles.AddAsync(new PermissionRole
                {
                    RoleId = roleId,
                    PermissionId = permissionId
                });

                await _dbContext.SaveChangesAsync();
                _permissionCache.Invalidate();
            }

            return await GetRoleByIdAsync(roleId);
        }

        public async Task<RoleDetailResponseDto> RevokeRolePermissionAsync(Guid roleId, Guid permissionId)
        {
            var role = await GetRoleWithPermissionsAsync(roleId);

            if (role == null)
                throw new NotFoundException("Not found role");

            await EnsurePermissionExistsAsync(permissionId);

            var permissionRole = role.PermissionRoles.FirstOrDefault(pr => pr.PermissionId == permissionId);
            if (permissionRole != null)
            {
                _dbContext.PermissionRoles.Remove(permissionRole);
                await _dbContext.SaveChangesAsync();
                _permissionCache.Invalidate();
            }

            return await GetRoleByIdAsync(roleId);
        }

        private async Task<Role?> GetRoleWithPermissionsAsync(Guid roleId)
        {
            return await _dbContext.Roles
                .Include(r => r.PermissionRoles)
                    .ThenInclude(pr => pr.Permission)
                .FirstOrDefaultAsync(r => r.RoleId == roleId);
        }

        private async Task EnsurePermissionExistsAsync(Guid permissionId)
        {
            var exists = await _dbContext.Permissions.AnyAsync(p => p.PermissionId == permissionId);

            if (!exists)
                throw new NotFoundException("Permission not found");
        }

        private async Task EnsurePermissionsExistAsync(IReadOnlySet<Guid> permissionIds)
        {
            if (permissionIds.Count == 0)
                return;

            var existingPermissionIds = await _dbContext.Permissions
                .Where(permission => permissionIds.Contains(permission.PermissionId))
                .Select(permission => permission.PermissionId)
                .ToListAsync();

            var missingPermissionIds = permissionIds
                .Except(existingPermissionIds)
                .ToList();

            if (missingPermissionIds.Count > 0)
                throw new NotFoundException("One or more permissions were not found");
        }

        private static RoleDetailResponseDto MapRoleDetail(Role role)
        {
            return new RoleDetailResponseDto
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName,
                Permissions = role.PermissionRoles
                    .Where(pr => pr.Permission != null)
                    .Select(pr => new PermissionResponseDto
                    {
                        PermissionId = pr.Permission.PermissionId,
                        PermissionName = pr.Permission.PermissionName
                    })
                    .OrderBy(permission => permission.PermissionName)
                    .ToList()
            };
        }

        private static string NormalizeRoleName(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                throw new ArgumentException("Role name is required");

            return roleName.Trim().ToLowerInvariant();
        }
    }
}
