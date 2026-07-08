using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RoadmapPlatform.Infrastructure.Data;

namespace RoadmapPlatform.Infrastructure.Security;

/// <summary>
/// Caches role-permission mappings used by permission authorization handlers.
/// </summary>
/// <remarks>
/// This cache prevents the authorization pipeline from querying the database
/// on every request when resolving permissions for user roles.
/// </remarks>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionCache"/> class.
    /// </summary>
    /// <param name="cache">The in-memory cache used to store role-permission mappings.</param>
    /// <param name="dbContext">The database context used to load roles and permissions.</param>
    public PermissionCache(IMemoryCache cache, ApplicationDbContext dbContext)
    {
        _cache = cache;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Gets the cached mapping of normalized role names to permission sets.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the database loading operation.</param>
    /// <returns>
    /// A read-only dictionary where each key is a normalized role name and each value
    /// is the set of permissions assigned to that role.
    /// </returns>
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
            role => role.RoleName.Trim().ToLowerInvariant(),
            role => (IReadOnlySet<string>)role.PermissionRoles
                .Where(permissionRole => permissionRole.Permission is not null)
                .Select(permissionRole => permissionRole.Permission!.PermissionName.Trim())
                .Where(permission => !string.IsNullOrWhiteSpace(permission))
                .ToHashSet(StringComparer.Ordinal));

        _cache.Set(CacheKey, permissionsMap, CacheOptions);

        return permissionsMap;
    }

    /// <summary>
    /// Removes the cached role-permission mapping so it can be reloaded from the database.
    /// </summary>
    public void Invalidate()
    {
        _cache.Remove(CacheKey);
    }
}
