using System.ComponentModel.DataAnnotations;

namespace RoadmapPlatform.Application.DTOs.Auth
{
    public class RegisterRequestDto
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(40, MinimumLength = 3,
                ErrorMessage = "Username must be between 3 and 40 characters")]
        [RegularExpression(@"^[a-zA-Z0-9._-]+$",
                ErrorMessage = "Username may only contain letters, numbers, ., _, and -")]
        public string? Username { get; set; } = string.Empty;

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
}