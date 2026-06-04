using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Infrastructure.Data;

namespace RoadmapPlatform.Infrastructure.Security;

public class PermissionCache : IPermissionCache
{
    private readonly IMemoryCache _cache;
    private readonly ApplicationDbContext _dbContext;
    private const string CacheKey = "RolePermissionsMap";

    public PermissionCache(IMemoryCache cache, ApplicationDbContext dbContext)
    {
        _cache = cache;
        _dbContext = dbContext;
    }

    public async Task<Dictionary<string, HashSet<string>>> GetPermissionsMapAsync()
    {
        return await _cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(24);
            
            var rolesWithPermissions = await _dbContext.Roles
                .Include(r => r.PermissionRoles)
                    .ThenInclude(pr => pr.Permission)
                .ToListAsync();

            return rolesWithPermissions.ToDictionary(
                r => r.RoleName,
                r => r.PermissionRoles
                    .Where(pr => pr.Permission != null)
                    .Select(pr => pr.Permission.PermissionName)
                    .ToHashSet()
            );
        }) ?? new Dictionary<string, HashSet<string>>();
    }

    public void Invalidate()
    {
        _cache.Remove(CacheKey);
    }
}
