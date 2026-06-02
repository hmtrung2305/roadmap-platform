using System.ComponentModel.DataAnnotations;

namespace RoadmapPlatform.Application.DTOs.AuthProviders;

public class ChangePasswordRequestDto
{
    [Required(ErrorMessage = "Current password is required")]
    public string? CurrentPassword { get; set; }

    [Required(ErrorMessage = "New password is required")]
    [MinLength(8)]
    [RegularExpression(
        "^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^a-zA-Z0-9]).{8,}",
        ErrorMessage = "Password must contain at least 8 characters with uppercase, lowercase, number, and special character.")]
    public string? NewPassword { get; set; }
}