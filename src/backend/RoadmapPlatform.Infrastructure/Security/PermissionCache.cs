using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RoadmapPlatform.Infrastructure.Data;

namespace RoadmapPlatform.Infrastructure.Security;

public sealed class PermissionCache : IPermissionCache
{
    private const string CacheKey = "rbac:role-permissions:v1";

    private static readonly MemoryCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15),
        SlidingExpiration = TimeSpan.FromMinutes(5)
    };

    private readonly IMemoryCache _cache;
    private readonly ApplicationDbContext _dbContext;

    public PermissionCache(IMemoryCache cache, ApplicationDbContext dbContext)
    {
        _cache = cache;
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlySet<string>>> GetPermissionsMapAsync(
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out IReadOnlyDictionary<string, IReadOnlySet<string>>? cached) &&
            cached is not null)
        {
            return cached;
        }

        var rolesWithPermissions = await _dbContext.Roles
            .AsNoTracking()
            .Include(role => role.PermissionRoles)
                .ThenInclude(permissionRole => permissionRole.Permission)
            .ToListAsync(cancellationToken);

        var permissionsMap = rolesWithPermissions.ToDictionary(
            role => role.RoleName.ToLowerInvariant(),
            role => (IReadOnlySet<string>)role.PermissionRoles
                .Where(permissionRole => permissionRole.Permission is not null)
                .Select(permissionRole => permissionRole.Permission!.PermissionName)
                .ToHashSet(StringComparer.Ordinal));

        _cache.Set(CacheKey, permissionsMap, CacheOptions);

        return permissionsMap;
    }

    public void Invalidate()
    {
        _cache.Remove(CacheKey);
    }
}
