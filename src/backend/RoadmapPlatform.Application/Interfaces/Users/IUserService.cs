using RoadmapPlatform.Application.DTOs.Users;

namespace RoadmapPlatform.Application.Interfaces.Users;

/// <summary>
/// Defines user account and profile operations for the current authenticated user.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Gets the current authenticated user's account information.
    /// </summary>
    /// <param name="userId">The current user's identifier.</param>
    /// <returns>The current user's account information.</returns>
    Task<CurrentUserResponseDto> GetCurrentUserAsync(Guid userId);

    /// <summary>
    /// Updates the current authenticated user's account information.
    /// </summary>
    /// <param name="userId">The current user's identifier.</param>
    /// <param name="request">The account update request.</param>
    /// <returns>The updated user account information.</returns>
    Task<UserResponseDto> UpdateCurrentUserAsync(Guid userId, UpdateCurrentUserRequestDto request);

    /// <summary>
    /// Gets the current authenticated user's profile information.
    /// </summary>
    /// <param name="userId">The current user's identifier.</param>
    /// <returns>The current user's profile information.</returns>
    Task<ProfileResponseDto> GetMyProfileAsync(Guid userId);

    /// <summary>
    /// Updates the current authenticated user's profile information.
    /// </summary>
    /// <param name="userId">The current user's identifier.</param>
    /// <param name="request">The profile update request.</param>
    /// <returns>The updated profile information.</returns>
    Task<ProfileResponseDto> UpdateMyProfileAsync(Guid userId, UpdateProfileRequestDto request);

    /// <summary>
    /// Deletes or deactivates the current authenticated user's account.
    /// </summary>
    /// <param name="userId">The current user's identifier.</param>
    Task DeleteAccountAsync(Guid userId);
}