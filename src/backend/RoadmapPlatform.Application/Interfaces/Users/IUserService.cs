using RoadmapPlatform.Application.DTOs.Users;

namespace RoadmapPlatform.Application.Interfaces.Users;

public interface IUserService
{
    Task<CurrentUserResponseDto> GetCurrentUserAsync(Guid userId);

    Task<UserResponseDto> UpdateCurrentUserAsync(Guid userId, UpdateCurrentUserRequestDto request);

    Task<ProfileResponseDto> GetMyProfileAsync(Guid userId);

    Task<ProfileResponseDto> UpdateMyProfileAsync(Guid userId, UpdateProfileRequestDto request);

    Task DeleteAccountAsync(Guid userId);
}