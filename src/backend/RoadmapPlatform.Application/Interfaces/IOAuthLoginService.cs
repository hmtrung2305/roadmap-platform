using RoadmapPlatform.Application.DTOs.AuthProviders;
using RoadmapPlatform.Application.DTOs.Users;

namespace RoadmapPlatform.Application.Interfaces;

public interface IOAuthLoginService
{
    Task<AuthenticatedUserDto> LoginOrCreateUserAsync(OAuthUserInfoDto externalLogin);
}
