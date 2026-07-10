using RoadmapPlatform.Application.DTOs.Users;

namespace RoadmapPlatform.Application.Interfaces.Users;

/// <summary>
/// Defines account profile operations for the current authenticated user.
/// </summary>
public interface IAccountProfileService
{
    /// <summary>
    /// Gets the account profile information for the specified user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The account profile information.</returns>
    Task<AccountProfileResponseDto> GetAccountProfileAsync(Guid userId);

    /// <summary>
    /// Updates the account profile information for the specified user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="request">The account profile update request.</param>
    /// <returns>The updated account profile information.</returns>
    Task<AccountProfileResponseDto> UpdateAccountProfileAsync(
        Guid userId,
        UpdateAccountProfileRequestDto request);
}