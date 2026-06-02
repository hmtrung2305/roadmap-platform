using System.ComponentModel.DataAnnotations;

namespace RoadmapPlatform.Application.DTOs.AuthProviders;

public class UpdateLocalEmailRequestDto
{
    [Required(ErrorMessage = "New email is required")]
    [EmailAddress]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        ErrorMessage = "Invalid email format.")]
    public string? NewEmail { get; set; }
}