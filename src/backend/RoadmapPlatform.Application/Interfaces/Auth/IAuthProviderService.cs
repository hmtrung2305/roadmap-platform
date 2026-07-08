using System.Security.Claims;
using RoadmapPlatform.Application.DTOs.AuthProviders;

namespace RoadmapPlatform.Application.Interfaces.Auth
{
    /// <summary>
    /// Defines account-level authentication provider management operations.
    /// </summary>
    /// <remarks>
    /// This interface is used for managing login methods linked to an existing user account.
    ///
    /// It is different from IAuthService:
    /// - IAuthService handles registration and login.
    /// - IAuthProviderService handles linking, unlinking, and updating login methods
    ///   for a user who already has an account.
    ///
    /// Example login providers:
    /// - Local email/password.
    /// - Google.
    /// - GitHub.
    /// </remarks>
    public interface IAuthProviderService
    {
        /// <summary>
        /// Gets the authentication provider status for a user.
        /// </summary>
        /// <param name="userId">
        /// The user ID whose login methods should be checked.
        /// </param>
        /// <param name="cancellationToken">
        /// A token used to cancel the operation.
        /// </param>
        /// <returns>
        /// A list of login method statuses, including whether each provider is linked,
        /// can be unlinked, or requires verification.
        /// </returns>
        Task<List<LoginMethodStatusDto>> GetAuthProviderStatusAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Links local email/password login to an existing user account.
        /// </summary>
        /// <param name="userId">
        /// The user ID that will receive the local login method.
        /// </param>
        /// <param name="request">
        /// The request containing local login information such as email and password.
        /// </param>
        /// <param name="cancellationToken">
        /// A token used to cancel the operation.
        /// </param>
        /// <returns>
        /// A response describing the result of linking local login.
        /// </returns>
        /// <remarks>
        /// The implementation may require email verification before the local login
        /// method becomes fully usable.
        /// </remarks>
        Task<LinkLocalLoginResponseDto> LinkLocalLoginAsync(
            Guid userId,
            LinkLocalLoginRequestDto request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Changes the local password for an existing user account.
        /// </summary>
        /// <param name="userId">
        /// The user ID requesting the password change.
        /// </param>
        /// <param name="request">
        /// The password change request.
        /// </param>
        /// <param name="cancellationToken">
        /// A token used to cancel the operation.
        /// </param>
        Task ChangePasswordAsync(
            Guid userId,
            ChangePasswordRequestDto request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Links a GitHub login identity to an existing user account.
        /// </summary>
        /// <param name="userId">
        /// The user ID that will be linked with GitHub.
        /// </param>
        /// <param name="githubUser">
        /// The authenticated GitHub claims principal returned by the OAuth middleware.
        /// </param>
        /// <param name="githubAccessToken">
        /// The GitHub OAuth access token, when available.
        /// </param>
        /// <param name="cancellationToken">
        /// A token used to cancel the operation.
        /// </param>
        Task LinkGitHubAsync(
            Guid userId,
            ClaimsPrincipal githubUser,
            string? githubAccessToken,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Links a Google login identity to an existing user account.
        /// </summary>
        /// <param name="userId">
        /// The user ID that will be linked with Google.
        /// </param>
        /// <param name="googleUser">
        /// The authenticated Google claims principal returned by the OAuth middleware.
        /// </param>
        /// <param name="cancellationToken">
        /// A token used to cancel the operation.
        /// </param>
        Task LinkGoogleAsync(
            Guid userId,
            ClaimsPrincipal googleUser,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Unlinks an authentication provider from an existing user account.
        /// </summary>
        /// <param name="userId">
        /// The user ID that owns the provider.
        /// </param>
        /// <param name="provider">
        /// The provider name to unlink.
        /// </param>
        /// <param name="cancellationToken">
        /// A token used to cancel the operation.
        /// </param>
        /// <remarks>
        /// The implementation should prevent removing the user's last usable login method.
        /// </remarks>
        Task UnlinkProviderAsync(
            Guid userId,
            string provider,
            CancellationToken cancellationToken = default);
    }
}