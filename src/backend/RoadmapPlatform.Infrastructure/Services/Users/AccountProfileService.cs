using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.Users;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.Users;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.Users;

/// <summary>
/// Provides account-profile operations for the authenticated user.
/// </summary>
public class AccountProfileService : IAccountProfileService
{
    private readonly ApplicationDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountProfileService"/> class.
    /// </summary>
    /// <param name="dbContext">The application database context.</param>
    public AccountProfileService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Gets the authenticated user's account-profile information.
    /// </summary>
    /// <param name="userId">The authenticated user's identifier.</param>
    /// <returns>The user's account-profile information.</returns>
    /// <exception cref="NotFoundException">Thrown when the user's profile does not exist.</exception>
    public async Task<AccountProfileResponseDto> GetAccountProfileAsync(Guid userId)
    {
        var profile = await _dbContext.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (profile == null)
        {
            throw new NotFoundException("User profile was not found");
        }

        return MapToAccountProfileResponse(profile);
    }

    /// <summary>
    /// Updates the authenticated user's account-profile fields.
    /// </summary>
    /// <param name="userId">The authenticated user's identifier.</param>
    /// <param name="request">The account-profile update request.</param>
    /// <returns>The updated account-profile information.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the request body is missing.</exception>
    /// <exception cref="NotFoundException">Thrown when the user's profile does not exist.</exception>
    public async Task<AccountProfileResponseDto> UpdateAccountProfileAsync(
        Guid userId,
        UpdateAccountProfileRequestDto request)
    {
        if (request == null)
        {
            throw new InvalidOperationException("Request body was not provided");
        }

        var profile = await _dbContext.UserProfiles
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (profile == null)
        {
            throw new NotFoundException("User profile was not found");
        }

        if (request.DisplayName != null)
        {
            profile.DisplayName = TrimOrNull(request.DisplayName);
        }

        if (request.AvatarUrl != null)
        {
            profile.AvatarUrl = TrimOrNull(request.AvatarUrl);
        }

        if (request.PhoneNumber != null)
        {
            profile.PhoneNumber = TrimOrNull(request.PhoneNumber);
        }

        profile.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return MapToAccountProfileResponse(profile);
    }

    /// <summary>
    /// Maps a user profile entity to an account-profile response DTO.
    /// </summary>
    /// <param name="profile">The user profile entity.</param>
    /// <returns>The mapped account-profile response.</returns>
    private static AccountProfileResponseDto MapToAccountProfileResponse(UserProfile profile)
    {
        return new AccountProfileResponseDto
        {
            DisplayName = profile.DisplayName,
            AvatarUrl = profile.AvatarUrl,
            PhoneNumber = profile.PhoneNumber
        };
    }

    /// <summary>
    /// Trims a string value and converts blank values to null.
    /// </summary>
    /// <param name="value">The input string value.</param>
    /// <returns>The trimmed value, or null when the value is blank.</returns>
    private static string? TrimOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}