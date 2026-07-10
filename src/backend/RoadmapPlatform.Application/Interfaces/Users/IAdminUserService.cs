using RoadmapPlatform.Application.DTOs.Users;

namespace RoadmapPlatform.Application.Interfaces.Users;

/// <summary>
/// Defines administrative user management operations.
/// </summary>
public interface IAdminUserService
{
    /// <summary>
    /// Gets users for administration, optionally filtered by a search keyword.
    /// </summary>
    /// <param name="search">An optional keyword used to filter users.</param>
    /// <returns>The list of users visible to administrators.</returns>
    Task<List<AdminUserResponseDto>> GetUsersAsync(string? search = null);

    /// <summary>
    /// Gets an administrative user detail by user identifier.
    /// </summary>
    /// <param name="userId">The target user identifier.</param>
    /// <returns>The administrative user detail.</returns>
    Task<AdminUserResponseDto> GetUserByIdAsync(Guid userId);

    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    /// <param name="userId">The target user identifier.</param>
    /// <param name="roleId">The role identifier to assign.</param>
    /// <returns>The updated administrative user detail.</returns>
    Task<AdminUserResponseDto> AssignUserRoleAsync(Guid userId, Guid roleId);

    /// <summary>
    /// Revokes a role from a user.
    /// </summary>
    /// <param name="userId">The target user identifier.</param>
    /// <param name="roleId">The role identifier to revoke.</param>
    /// <param name="actorUserId">The identifier of the administrator performing the action.</param>
    /// <returns>The updated administrative user detail.</returns>
    Task<AdminUserResponseDto> RevokeUserRoleAsync(Guid userId, Guid roleId, Guid actorUserId);
}