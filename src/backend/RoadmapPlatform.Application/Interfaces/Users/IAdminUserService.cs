using RoadmapPlatform.Application.DTOs.Users;

namespace RoadmapPlatform.Application.Interfaces.Users;

public interface IAdminUserService
{
    Task<List<AdminUserResponseDto>> GetUsersAsync(string? search = null);

    Task<AdminUserResponseDto> GetUserByIdAsync(Guid userId);

    Task<AdminUserResponseDto> AssignUserRoleAsync(Guid userId, Guid roleId);

    Task<AdminUserResponseDto> RevokeUserRoleAsync(Guid userId, Guid roleId, Guid actorUserId);
}
