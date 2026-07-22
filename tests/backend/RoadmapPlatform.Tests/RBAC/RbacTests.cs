using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using RoadmapPlatform.Infrastructure.Security;

namespace RoadmapPlatform.Tests;

public sealed class RbacTests
{
    private const string Permission = "skills.manage";

    [Fact]
    public async Task TC063_AuthorizedRole_WithRequiredPermission_SucceedsAuthorization()
    {
        var cache = new MutablePermissionCache();
        cache.SetRolePermissions("content_manager", Permission);
        var context = CreateContext(CreateAuthenticatedUser("content_manager"), Permission);

        await new PermissionHandler(cache).HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task TC064_RoleMissingPermission_IsDeniedAuthorization()
    {
        var cache = new MutablePermissionCache();
        cache.SetRolePermissions("learner", "roadmaps.read");
        var context = CreateContext(CreateAuthenticatedUser("learner"), Permission);

        await new PermissionHandler(cache).HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task TC065_UnauthenticatedPrincipal_IsDeniedWithoutLoadingProtectedPermissions()
    {
        var cache = new MutablePermissionCache();
        cache.SetRolePermissions("content_manager", Permission);
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        var context = CreateContext(anonymous, Permission);

        await new PermissionHandler(cache).HandleAsync(context);

        Assert.False(context.HasSucceeded);
        Assert.Equal(0, cache.GetCallCount);
    }

    [Fact]
    public async Task TC082_PermissionRevokedDuringActiveSession_IsDeniedOnNextAuthorizationCheck()
    {
        var cache = new MutablePermissionCache();
        cache.SetRolePermissions("content_manager", Permission);
        var principal = CreateAuthenticatedUser("content_manager");
        var handler = new PermissionHandler(cache);

        var beforeRevocation = CreateContext(principal, Permission);
        await handler.HandleAsync(beforeRevocation);
        Assert.True(beforeRevocation.HasSucceeded);

        cache.SetRolePermissions("content_manager");
        var afterRevocation = CreateContext(principal, Permission);
        await handler.HandleAsync(afterRevocation);

        Assert.False(afterRevocation.HasSucceeded);
    }

    private static ClaimsPrincipal CreateAuthenticatedUser(string role)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()), new Claim(ClaimTypes.Role, role)],
            authenticationType: "unit-test");
        return new ClaimsPrincipal(identity);
    }

    private static AuthorizationHandlerContext CreateContext(ClaimsPrincipal principal, string permission)
    {
        var requirement = new PermissionRequirement(permission);
        return new AuthorizationHandlerContext([requirement], principal, resource: null);
    }

    private sealed class MutablePermissionCache : IPermissionCache
    {
        private readonly Dictionary<string, IReadOnlySet<string>> _map = new(StringComparer.Ordinal);

        public int GetCallCount { get; private set; }

        public void SetRolePermissions(string role, params string[] permissions)
        {
            _map[role.Trim().ToLowerInvariant()] = permissions.ToHashSet(StringComparer.Ordinal);
        }

        public Task<IReadOnlyDictionary<string, IReadOnlySet<string>>> GetPermissionsMapAsync(
            CancellationToken cancellationToken = default)
        {
            GetCallCount++;
            return Task.FromResult<IReadOnlyDictionary<string, IReadOnlySet<string>>>(_map);
        }

        public void Invalidate()
        {
        }
    }
}
