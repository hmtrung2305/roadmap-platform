namespace RoadmapPlatform.Infrastructure.Security;

public interface IPermissionCache
{
    Task<IReadOnlyDictionary<string, IReadOnlySet<string>>> GetPermissionsMapAsync(
        CancellationToken cancellationToken = default);

    void Invalidate();
}
