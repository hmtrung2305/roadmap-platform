using RoadmapPlatform.Application.DTOs.Users;

namespace RoadmapPlatform.Application.DTOs.Auth;

public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public UserResponseDto User { get; set; } = new();
}