using System.ComponentModel.DataAnnotations;
using RoadmapPlatform.Application.Interfaces.Security;

namespace RoadmapPlatform.Application.DTOs.Auth;

public class LoginRequestDto : ICaptchaProtectedRequest
{
    [Required(ErrorMessage = "Email or username is required")]
    public string? EmailOrUsername { get; set; }

    [Required(ErrorMessage = "Password is required")]
    public string? Password { get; set; }

    public string? CaptchaToken { get; set; }
}