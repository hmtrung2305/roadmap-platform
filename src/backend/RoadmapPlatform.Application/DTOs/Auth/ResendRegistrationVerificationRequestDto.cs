using System.ComponentModel.DataAnnotations;

namespace RoadmapPlatform.Application.DTOs.Auth;

public class ResendRegistrationVerificationRequestDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    public string? Email { get; set; }
}