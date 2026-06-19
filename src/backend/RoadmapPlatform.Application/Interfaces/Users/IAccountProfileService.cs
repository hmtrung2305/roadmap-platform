using RoadmapPlatform.Application.DTOs.Users;

namespace RoadmapPlatform.Application.Interfaces.Users;

public interface IAccountProfileService
{
    Task<AccountProfileResponseDto> GetAccountProfileAsync(Guid userId);

    Task<AccountProfileResponseDto> UpdateAccountProfileAsync(
        Guid userId,
        UpdateAccountProfileRequestDto request);
}
