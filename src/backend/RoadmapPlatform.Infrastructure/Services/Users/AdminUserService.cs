using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.Role;
using RoadmapPlatform.Application.DTOs.Users;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.Users;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.Users;

public sealed class AdminUserService : IAdminUserService
{
    private readonly ApplicationDbContext _dbContext;

    public AdminUserService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<AdminUserResponseDto>> GetUsersAsync(string? search = null)
    {
        var normalizedSearch = NormalizeSearch(search);

        var query = _dbContext.Users
            .AsNoTracking()
            .Include(user => user.UserAuthProviders)
            .Include(user => user.UserRoles)
                .ThenInclude(userRole => userRole.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(user =>
                user.UsernameNormalized.Contains(normalizedSearch) ||
                user.UserAuthProviders.Any(provider => provider.Email != null &&
                    provider.Email.ToLower().Contains(normalizedSearch)));
        }

        var users = await query
            .OrderBy(user => user.UsernameNormalized)
            .Take(200)
            .ToListAsync();

        return users.Select(MapUser).ToList();
    }

    public async Task<AdminUserResponseDto> GetUserByIdAsync(Guid userId)
    {
        var user = await GetUserWithRolesAsync(userId);

        if (user == null)
            throw new NotFoundException("User was not found");

        return MapUser(user);
    }

    public async Task<AdminUserResponseDto> AssignUserRoleAsync(Guid userId, Guid roleId)
    {
        var user = await GetUserWithRolesAsync(userId);

        if (user == null)
            throw new NotFoundException("User was not found");

        await EnsureRoleExistsAsync(roleId);

        var isAssigned = user.UserRoles.Any(userRole => userRole.RoleId == roleId);
        if (!isAssigned)
        {
            await _dbContext.UserRoles.AddAsync(new UserRole
            {
                UserId = userId,
                RoleId = roleId
            });

            await _dbContext.SaveChangesAsync();
        }

        return await GetUserByIdAsync(userId);
    }

    public async Task<AdminUserResponseDto> RevokeUserRoleAsync(Guid userId, Guid roleId)
    {
        var user = await GetUserWithRolesAsync(userId);

        if (user == null)
            throw new NotFoundException("User was not found");

        await EnsureRoleExistsAsync(roleId);

        var userRole = user.UserRoles.FirstOrDefault(assignment => assignment.RoleId == roleId);
        if (userRole != null)
        {
            _dbContext.UserRoles.Remove(userRole);
            await _dbContext.SaveChangesAsync();
        }

        return await GetUserByIdAsync(userId);
    }

    private async Task<User?> GetUserWithRolesAsync(Guid userId)
    {
        return await _dbContext.Users
            .Include(user => user.UserAuthProviders)
            .Include(user => user.UserRoles)
                .ThenInclude(userRole => userRole.Role)
            .FirstOrDefaultAsync(user => user.UserId == userId);
    }

    private async Task EnsureRoleExistsAsync(Guid roleId)
    {
        var exists = await _dbContext.Roles.AnyAsync(role => role.RoleId == roleId);

        if (!exists)
            throw new NotFoundException("Role was not found");
    }

    private static AdminUserResponseDto MapUser(User user)
    {
        return new AdminUserResponseDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.UserAuthProviders
                .OrderByDescending(provider => provider.Provider == "local")
                .ThenBy(provider => provider.Provider)
                .Select(provider => provider.Email)
                .FirstOrDefault(),
            Status = user.Status,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Roles = user.UserRoles
                .Where(userRole => userRole.Role != null)
                .Select(userRole => new RoleResponseDto
                {
                    RoleId = userRole.Role.RoleId,
                    RoleName = userRole.Role.RoleName
                })
                .OrderBy(role => role.RoleName)
                .ToList()
        };
    }

    private static string? NormalizeSearch(string? search)
    {
        return string.IsNullOrWhiteSpace(search)
            ? null
            : search.Trim().ToLowerInvariant();
    }
}
