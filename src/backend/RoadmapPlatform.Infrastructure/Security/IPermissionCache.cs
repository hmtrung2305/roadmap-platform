namespace RoadmapPlatform.Infrastructure.Security;

public interface IPermissionCache
{
    Task<Dictionary<string, HashSet<string>>> GetPermissionsMapAsync();
    void Invalidate();
}
