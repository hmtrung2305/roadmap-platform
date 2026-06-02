using System.ComponentModel.DataAnnotations;

namespace RoadmapPlatform.Application.DTOs.Auth;

public class VerifyRegistrationEmailRequestDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    public string? Email { get; set; }

    [Required(ErrorMessage = "OTP is required")]
    public string? Otp { get; set; }
}