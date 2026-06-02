using System.ComponentModel.DataAnnotations;

namespace RoadmapPlatform.Application.DTOs.AuthProviders;

public class LinkLocalLoginRequestDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        ErrorMessage = "Invalid email format.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [MinLength(8)]
    [RegularExpression(
        "^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^a-zA-Z0-9]).{8,}",
        ErrorMessage = "Password must contain at least 8 characters with uppercase, lowercase, number, and special character.")]
    public string? Password { get; set; }
}