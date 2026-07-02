using RoadmapPlatform.Application.DTOs.Role;

namespace RoadmapPlatform.Application.DTOs.Users;

public class AdminUserResponseDto
{
    public Guid UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<RoleResponseDto> Roles { get; set; } = new();
}
