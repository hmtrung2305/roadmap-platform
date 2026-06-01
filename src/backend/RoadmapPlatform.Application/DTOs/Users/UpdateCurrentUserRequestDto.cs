using System.ComponentModel.DataAnnotations;

namespace RoadmapPlatform.Application.DTOs.Users;

public class UpdateCurrentUserRequestDto
{
    [Required(ErrorMessage = "Username is required")]
    [StringLength(40, MinimumLength = 3,
           ErrorMessage = "Username must be between 3 and 40 characters")]
    [RegularExpression(@"^[a-zA-Z0-9._-]+$",
           ErrorMessage = "Username may only contain letters, numbers, ., _, and -")]
    public string? Username { get; set; } = string.Empty;
}