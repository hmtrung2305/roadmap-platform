using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.Role;
using RoadmapPlatform.Application.DTOs.Users;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.Users;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.Users;

/// <summary>
/// Provides administrative user management operations, including user lookup
/// and user-role assignment management.
/// </summary>
public sealed class AdminUserService : IAdminUserService
{
    private readonly ApplicationDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminUserService"/> class.
    /// </summary>
    /// <param name="dbContext">The application database context.</param>
    public AdminUserService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Gets a limited list of users for administrative management.
    /// Optionally filters users by username or authentication provider email.
    /// </summary>
    /// <param name="search">An optional search keyword for username or email.</param>
    /// <returns>A list of users with their roles and basic account information.</returns>
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

    /// <summary>
    /// Gets a single user's administrative details, including assigned roles.
    /// </summary>
    /// <param name="userId">The target user's identifier.</param>
    /// <returns>The user's administrative details.</returns>
    /// <exception cref="NotFoundException">Thrown when the user does not exist.</exception>
    public async Task<AdminUserResponseDto> GetUserByIdAsync(Guid userId)
    {
        var user = await GetUserWithRolesAsync(userId);

        if (user == null)
            throw new NotFoundException("User was not found");

        return MapUser(user);
    }

    /// <summary>
    /// Assigns a role to a user if the role is not already assigned.
    /// </summary>
    /// <param name="userId">The target user's identifier.</param>
    /// <param name="roleId">The role identifier to assign.</param>
    /// <returns>The updated administrative user details.</returns>
    /// <exception cref="NotFoundException">Thrown when the user or role does not exist.</exception>
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

    /// <summary>
    /// Revokes a role from a user if the assignment exists.
    /// Prevents an actor from revoking their own admin role.
    /// </summary>
    /// <param name="userId">The target user's identifier.</param>
    /// <param name="roleId">The role identifier to revoke.</param>
    /// <param name="actorUserId">The authenticated admin user's identifier.</param>
    /// <returns>The updated administrative user details.</returns>
    /// <exception cref="NotFoundException">Thrown when the user or role does not exist.</exception>
    /// <exception cref="ForbiddenException">Thrown when the actor tries to revoke their own admin role.</exception>
    public async Task<AdminUserResponseDto> RevokeUserRoleAsync(Guid userId, Guid roleId, Guid actorUserId)
    {
        var user = await GetUserWithRolesAsync(userId);

        if (user == null)
            throw new NotFoundException("User was not found");

        var role = await GetRoleOrThrowAsync(roleId);

        if (userId == actorUserId && IsAdminRole(role))
            throw new ForbiddenException("You cannot revoke your own admin role.");

        var userRole = user.UserRoles.FirstOrDefault(assignment => assignment.RoleId == roleId);
        if (userRole != null)
        {
            _dbContext.UserRoles.Remove(userRole);
            await _dbContext.SaveChangesAsync();
        }

        return await GetUserByIdAsync(userId);
    }

    /// <summary>
    /// Loads a user with authentication providers and assigned roles.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The user entity when found; otherwise, null.</returns>
    private async Task<User?> GetUserWithRolesAsync(Guid userId)
    {
        return await _dbContext.Users
            .Include(user => user.UserAuthProviders)
            .Include(user => user.UserRoles)
                .ThenInclude(userRole => userRole.Role)
            .FirstOrDefaultAsync(user => user.UserId == userId);
    }

    /// <summary>
    /// Ensures that a role exists.
    /// </summary>
    /// <param name="roleId">The role identifier.</param>
    /// <exception cref="NotFoundException">Thrown when the role does not exist.</exception>
    private async Task EnsureRoleExistsAsync(Guid roleId)
    {
        await GetRoleOrThrowAsync(roleId);
    }

    /// <summary>
    /// Gets a role by identifier or throws when it does not exist.
    /// </summary>
    /// <param name="roleId">The role identifier.</param>
    /// <returns>The role entity.</returns>
    /// <exception cref="NotFoundException">Thrown when the role does not exist.</exception>
    private async Task<Role> GetRoleOrThrowAsync(Guid roleId)
    {
        var role = await _dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.RoleId == roleId);

        if (role == null)
            throw new NotFoundException("Role was not found");

        return role;
    }

    /// <summary>
    /// Determines whether the given role is the built-in admin role.
    /// </summary>
    /// <param name="role">The role to check.</param>
    /// <returns>True when the role is the admin role; otherwise, false.</returns>
    private static bool IsAdminRole(Role role)
    {
        return string.Equals(role.RoleName, RoleNames.Admin, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Maps a user entity to an administrative user response DTO.
    /// </summary>
    /// <param name="user">The user entity to map.</param>
    /// <returns>The mapped administrative user response.</returns>
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

    /// <summary>
    /// Normalizes a search keyword for case-insensitive user search.
    /// </summary>
    /// <param name="search">The raw search keyword.</param>
    /// <returns>The normalized search keyword, or null when the keyword is empty.</returns>
    private static string? NormalizeSearch(string? search)
    {
        return string.IsNullOrWhiteSpace(search)
            ? null
            : search.Trim().ToLowerInvariant();
    }
}