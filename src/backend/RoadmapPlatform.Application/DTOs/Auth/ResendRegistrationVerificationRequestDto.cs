using System.ComponentModel.DataAnnotations;
using RoadmapPlatform.Application.Interfaces.Security;

namespace RoadmapPlatform.Application.DTOs.Auth;

public class ResendRegistrationVerificationRequestDto : ICaptchaProtectedRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        ErrorMessage = "Invalid email format.")]
    public string? Email { get; set; }

    public string? CaptchaToken { get; set; }
}