using RoadmapPlatform.Application.DTOs.AuthProviders;
using System.Security.Claims;

namespace RoadmapPlatform.Application.Interfaces.Auth
{
    public interface IAuthProviderService
    {
        Task<List<LoginMethodStatusDto>> GetAuthProviderStatusAsync(Guid userId);
        Task LinkLocalLoginAsync(Guid userId, LinkLocalLoginRequestDto request);
        Task ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request);
        Task LinkGitHubAsync(Guid userId, ClaimsPrincipal githubUser);
        Task LinkGoogleAsync(Guid userId, ClaimsPrincipal googleUser);
        Task UnlinkProviderAsync(Guid userId, string provider);
    }
}
