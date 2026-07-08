using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.Permissions;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.Identity;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Security;

namespace RoadmapPlatform.Infrastructure.Services.Identity
{
    /// <summary>
    /// Provides permission catalog management operations.
    /// </summary>
    public class PermissionService : IPermissionService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IPermissionCache _permissionCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="PermissionService"/> class.
        /// </summary>
        /// <param name="dbContext">The application database context.</param>
        /// <param name="permissionCache">The permission cache used by authorization handlers.</param>
        public PermissionService(ApplicationDbContext dbContext, IPermissionCache permissionCache)
        {
            _dbContext = dbContext;
            _permissionCache = permissionCache;
        }

        /// <summary>
        /// Creates a new custom permission.
        /// </summary>
        /// <param name="createPermissionRequest">The permission creation request.</param>
        /// <returns>The created permission.</returns>
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

        /// <summary>
        /// Deletes a custom permission when it is safe to remove.
        /// </summary>
        /// <param name="permissionId">The permission identifier.</param>
        public async Task DeletePermissionAsync(Guid permissionId)
        {
            var permission = await _dbContext.Permissions
                .Include(p => p.PermissionRoles)
                    .ThenInclude(permissionRole => permissionRole.Role)
                .FirstOrDefaultAsync(p => p.PermissionId == permissionId);

            if (permission == null) throw new NotFoundException("Permission not found");
            EnsurePermissionCanBeRenamedOrDeleted(permission);

            if (permission.PermissionRoles.Count > 0)
            {
                _dbContext.PermissionRoles.RemoveRange(permission.PermissionRoles);
            }

            _dbContext.Permissions.Remove(permission);

            await _dbContext.SaveChangesAsync();
            _permissionCache.Invalidate();
        }

        /// <summary>
        /// Gets a permission by identifier.
        /// </summary>
        /// <param name="permissionId">The permission identifier.</param>
        /// <returns>The permission details.</returns>
        public async Task<PermissionResponseDto> GetPermissionByIdAsync(Guid permissionId)
        {
            var permission = await _dbContext.Permissions.FindAsync(permissionId);
            if (permission == null) throw new NotFoundException("Permission not found");

            return new PermissionResponseDto
            {
                PermissionId = permission.PermissionId,
                PermissionName = permission.PermissionName
            };
        }

        /// <summary>
        /// Gets all permissions ordered by permission name.
        /// </summary>
        /// <returns>The permission list.</returns>
        public async Task<List<PermissionResponseDto>> GetPermissionsAsync()
        {
            var permissions = await _dbContext.Permissions
                .AsNoTracking()
                .OrderBy(p => p.PermissionName)
                .ToListAsync();

            return permissions.Select(p => new PermissionResponseDto
            {
                PermissionId = p.PermissionId,
                PermissionName = p.PermissionName,
            }).ToList();
        }

        /// <summary>
        /// Updates a custom permission name when it is safe to rename.
        /// </summary>
        /// <param name="permissionId">The permission identifier.</param>
        /// <param name="updatePermissionRequest">The permission update request.</param>
        /// <returns>The updated permission.</returns>
        public async Task<PermissionResponseDto> UpdatePermissionAsync(Guid permissionId, UpdatePermissionRequestDto updatePermissionRequest)
        {
            var permissionName = NormalizePermissionName(updatePermissionRequest.PermissionName);
            var permission = await _dbContext.Permissions
                .Include(p => p.PermissionRoles)
                    .ThenInclude(permissionRole => permissionRole.Role)
                .FirstOrDefaultAsync(p => p.PermissionId == permissionId);

            if (permission == null) throw new NotFoundException("Permission not found");
            EnsurePermissionCanBeRenamedOrDeleted(permission);

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

        /// <summary>
        /// Normalizes and validates a permission name.
        /// </summary>
        /// <param name="permissionName">The raw permission name.</param>
        /// <returns>The normalized permission name.</returns>
        private static string NormalizePermissionName(string permissionName)
        {
            if (string.IsNullOrWhiteSpace(permissionName))
                throw new ArgumentException("Permission name is required");

            var normalizedPermissionName = permissionName.Trim().ToLowerInvariant();

            if (normalizedPermissionName.Split('.').Length != 3)
                throw new ArgumentException("Permission name must follow resource.action.scope");

            return normalizedPermissionName;
        }

        /// <summary>
        /// Ensures that a permission can be renamed or deleted.
        /// </summary>
        /// <param name="permission">The permission to validate.</param>
        private static void EnsurePermissionCanBeRenamedOrDeleted(Permission permission)
        {
            if (PermissionConstant.All.Contains(permission.PermissionName))
                throw new ForbiddenException("System permissions cannot be renamed or deleted.");

            if (permission.PermissionRoles.Any(permissionRole =>
                permissionRole.Role != null &&
                string.Equals(permissionRole.Role.RoleName, RoleNames.Admin, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ForbiddenException("Permissions assigned to the built-in admin role cannot be renamed or deleted.");
            }
        }
    }
}
