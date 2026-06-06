using System.ComponentModel.DataAnnotations;
using RoadmapPlatform.Application.Interfaces.Security;

namespace RoadmapPlatform.Application.DTOs.Auth;

public class ResendRegistrationVerificationRequestDto : ICaptchaProtectedRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    public string? Email { get; set; }

    public string? CaptchaToken { get; set; }
}