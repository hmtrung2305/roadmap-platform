using RoadmapPlatform.Application.DTOs.AuthProviders;
using System.Security.Claims;

namespace RoadmapPlatform.Application.Interfaces.Auth
{
    public interface IAuthProviderService
    {
        Task<List<LoginMethodStatusDto>> GetAuthProviderStatusAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task LinkLocalLoginAsync(
            Guid userId,
            LinkLocalLoginRequestDto request,
            CancellationToken cancellationToken = default);

        Task ChangePasswordAsync(
            Guid userId,
            ChangePasswordRequestDto request,
            CancellationToken cancellationToken = default);

        Task LinkGitHubAsync(
            Guid userId,
            ClaimsPrincipal githubUser,
            CancellationToken cancellationToken = default);

        Task LinkGoogleAsync(
            Guid userId,
            ClaimsPrincipal googleUser,
            CancellationToken cancellationToken = default);

        Task UnlinkProviderAsync(
            Guid userId,
            string provider,
            CancellationToken cancellationToken = default);
    }
}
