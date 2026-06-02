namespace RoadmapPlatform.Application.Interfaces.Auth;

public interface IJwtTokenService
{
    string GenerateToken(Guid userId, string username);
}
