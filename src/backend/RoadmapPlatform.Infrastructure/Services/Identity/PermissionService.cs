using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.Permissions;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.Identity;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Security;

namespace RoadmapPlatform.Infrastructure.Services.Identity
{
    public class PermissionService : IPermissionService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IPermissionCache _permissionCache;

        public PermissionService(ApplicationDbContext dbContext, IPermissionCache permissionCache)
        {
            _dbContext = dbContext;
            _permissionCache = permissionCache;
        }

        public async Task<PermissionResponseDto> CreatePermissionAsync(CreatePermissionRequestDto createPermissionRequest)
        {
            var permissionName = NormalizePermissionName(createPermissionRequest.PermissionName);
            var existedPermission = await _dbContext.Permissions
                                    .AnyAsync(p => p.PermissionName == permissionName);

            if (existedPermission) throw new ConflictException("Permission name already exists");

            var permission = new Permission
            {
                PermissionName = permissionName
            };

            _dbContext.Permissions.Add(permission);

            await _dbContext.SaveChangesAsync();
            _permissionCache.Invalidate();

            return new PermissionResponseDto
            {
                PermissionId = permission.PermissionId,
                PermissionName = permission.PermissionName
            };
        }

        public async Task DeletePermissionAsync(Guid permissionId)
        {
            var permission = await _dbContext.Permissions
                .Include(p => p.PermissionRoles)
                .FirstOrDefaultAsync(p => p.PermissionId == permissionId);

            if (permission == null) throw new NotFoundException("Permission not found");

            if (permission.PermissionRoles.Count > 0)
            {
                _dbContext.PermissionRoles.RemoveRange(permission.PermissionRoles);
            }

            _dbContext.Permissions.Remove(permission);

            await _dbContext.SaveChangesAsync();
            _permissionCache.Invalidate();
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
            var permissions = await _dbContext.Permissions
                .AsNoTracking()
                .OrderBy(p => p.PermissionName)
                .ToListAsync();
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
            var permissionName = NormalizePermissionName(updatePermissionRequest.PermissionName);
            var permission = await _dbContext.Permissions.FindAsync(permissionId);
            if (permission == null) throw new NotFoundException("Not Found Permissions");


            var existedPermission = await _dbContext.Permissions.AnyAsync(p =>
                                    p.PermissionName == permissionName &&
                                    p.PermissionId != permissionId);

            if (existedPermission) throw new ConflictException("Permission name already exists");

            permission.PermissionName = permissionName;

            await _dbContext.SaveChangesAsync();
            _permissionCache.Invalidate();

            return new PermissionResponseDto
            {
                PermissionId = permission.PermissionId,
                PermissionName = permission.PermissionName
            };
        }

        private static string NormalizePermissionName(string permissionName)
        {
            if (string.IsNullOrWhiteSpace(permissionName))
                throw new ArgumentException("Permission name is required");

            var normalizedPermissionName = permissionName.Trim().ToLowerInvariant();

            if (normalizedPermissionName.Split('.').Length != 3)
                throw new ArgumentException("Permission name must follow resource.action.scope");

            return normalizedPermissionName;
        }
    }
}
