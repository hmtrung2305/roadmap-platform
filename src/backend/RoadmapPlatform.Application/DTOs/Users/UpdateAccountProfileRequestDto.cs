using System.ComponentModel.DataAnnotations;

namespace RoadmapPlatform.Application.DTOs.Users;

public class UpdateAccountProfileRequestDto
{
    [StringLength(50, ErrorMessage = "Display name cannot exceed 50 characters")]
    public string? DisplayName { get; set; }

    [Url(ErrorMessage = "Avatar URL must be a valid URL")]
    public string? AvatarUrl { get; set; }

    [StringLength(32, ErrorMessage = "Phone number cannot exceed 32 characters")]
    public string? PhoneNumber { get; set; }
}
