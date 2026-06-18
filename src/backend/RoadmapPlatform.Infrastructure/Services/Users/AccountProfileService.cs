using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.Users;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.Users;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.Users;

public class AccountProfileService : IAccountProfileService
{
    private readonly ApplicationDbContext _dbContext;

    public AccountProfileService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

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

    private static AccountProfileResponseDto MapToAccountProfileResponse(UserProfile profile)
    {
        return new AccountProfileResponseDto
        {
            DisplayName = profile.DisplayName,
            AvatarUrl = profile.AvatarUrl,
            PhoneNumber = profile.PhoneNumber
        };
    }

    private static string? TrimOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
