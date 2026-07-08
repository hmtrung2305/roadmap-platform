namespace RoadmapPlatform.Infrastructure.Security;

/// <summary>
/// Provides cached role-permission mappings for authorization handlers.
/// </summary>
/// <remarks>
/// Authorization handlers use this cache to resolve which permissions are assigned
/// to each role without querying the database on every request.
/// </remarks>
public interface IPermissionCache
{
    /// <summary>
    /// Gets the cached mapping of role names to permission sets.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A dictionary where each key is a normalized role name and each value is the set
    /// of permissions assigned to that role.
    /// </returns>
    Task<IReadOnlyDictionary<string, IReadOnlySet<string>>> GetPermissionsMapAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates the cached permission map so it can be reloaded on the next request.
    /// </summary>
    void Invalidate();
}
