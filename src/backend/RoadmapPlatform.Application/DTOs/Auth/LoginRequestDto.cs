using System.ComponentModel.DataAnnotations;

namespace RoadmapPlatform.Application.DTOs.Auth;

public class LoginRequestDto
{
    [Required(ErrorMessage = "Email or username is required")]
    public string? EmailOrUsername { get; set; }

    [Required(ErrorMessage = "Password is required")]
    public string? Password { get; set; }
}