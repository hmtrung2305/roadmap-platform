using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.PermissionRole;
using RoadmapPlatform.Application.DTOs.Role;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Security;
using RoadmapPlatform.Infrastructure.Services.Identity;

namespace RoadmapPlatform.Tests;

public sealed class RoleAdministrationTests
{
    [Fact]
    public async Task TC072_CreateCustomRole_WithUniqueName_CreatesNormalizedRole()
    {
        await using var db = TestDbContextFactory.Create();
        var cache = new RecordingPermissionCache();
        var service = new RoleService(db, cache);

        var result = await service.CreateRoleAsync(new CreateRoleRequestDto { RoleName = "  QA Lead  " });

        Assert.Equal("qa lead", result.RoleName);
        Assert.True(await db.Roles.AnyAsync(role => role.RoleName == "qa lead"));
        Assert.Equal(1, cache.InvalidateCallCount);
    }

    [Fact]
    public async Task TC073_CreateCustomRole_WithDuplicateName_ThrowsConflict()
    {
        await using var db = TestDbContextFactory.Create();
        db.Roles.Add(TestEntityFactory.CreateRole("reviewer"));
        await db.SaveChangesAsync();
        var service = new RoleService(db, new RecordingPermissionCache());

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateRoleAsync(new CreateRoleRequestDto { RoleName = "REVIEWER" }));

        Assert.Equal(1, await db.Roles.CountAsync());
    }

    [Fact]
    public async Task TC074_RenameCustomRole_WithUniqueName_PreservesAssignments()
    {
        await using var db = TestDbContextFactory.Create();
        var user = TestEntityFactory.CreateUser("assigned-user");
        var role = TestEntityFactory.CreateRole("old-role");
        var assignment = LinkUserRole(user, role);
        db.AddRange(user, role, assignment);
        await db.SaveChangesAsync();
        var service = new RoleService(db, new RecordingPermissionCache());

        var result = await service.UpdateRoleAsync(role.RoleId, new UpdateRoleRequestDto { RoleName = "new-role" });

        Assert.Equal("new-role", result.RoleName);
        Assert.Equal(1, await db.UserRoles.CountAsync());
        Assert.Equal(role.RoleId, (await db.UserRoles.SingleAsync()).RoleId);
    }

    [Fact]
    public async Task TC075_DeleteUnusedCustomRole_RemovesRole()
    {
        await using var db = TestDbContextFactory.Create();
        var role = TestEntityFactory.CreateRole("temporary-role");
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        var service = new RoleService(db, new RecordingPermissionCache());

        await service.DeleteRoleAsync(role.RoleId);

        Assert.False(await db.Roles.AnyAsync(item => item.RoleId == role.RoleId));
    }

    [Fact]
    public async Task TC076_AssignPermissionsToCustomRole_AttachesSelectedPermissions()
    {
        await using var db = TestDbContextFactory.Create();
        var role = TestEntityFactory.CreateRole("content-editor");
        var first = TestEntityFactory.CreatePermission("skills.manage");
        var second = TestEntityFactory.CreatePermission("resources.manage");
        db.AddRange(role, first, second);
        await db.SaveChangesAsync();
        var service = new RoleService(db, new RecordingPermissionCache());

        var result = await service.AssignRolePermissionsAsync(role.RoleId, new AssignPermissionRoleRequestDto
        {
            PermissionIds = [first.PermissionId, second.PermissionId, first.PermissionId],
        });

        Assert.Equal(2, result.Permissions.Count);
        Assert.Equal(2, await db.PermissionRoles.CountAsync());
    }

    [Fact]
    public async Task TC077_RevokePermissionFromCustomRole_RemovesPermission()
    {
        await using var db = TestDbContextFactory.Create();
        var role = TestEntityFactory.CreateRole("content-editor");
        var permission = TestEntityFactory.CreatePermission("skills.manage");
        var link = LinkPermission(role, permission);
        db.AddRange(role, permission, link);
        await db.SaveChangesAsync();
        var service = new RoleService(db, new RecordingPermissionCache());

        var result = await service.RevokeRolePermissionAsync(role.RoleId, permission.PermissionId);

        Assert.Empty(result.Permissions);
        Assert.Empty(db.PermissionRoles);
    }

    [Fact]
    public async Task TC078_RenameBuiltInAdminRole_IsRejected()
    {
        await using var db = TestDbContextFactory.Create();
        var adminRole = TestEntityFactory.CreateRole(RoleNames.Admin);
        db.Roles.Add(adminRole);
        await db.SaveChangesAsync();
        var service = new RoleService(db, new RecordingPermissionCache());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.UpdateRoleAsync(adminRole.RoleId, new UpdateRoleRequestDto { RoleName = "super-admin" }));

        Assert.Equal(RoleNames.Admin, (await db.Roles.FindAsync(adminRole.RoleId))!.RoleName);
    }

    [Fact]
    public async Task TC079_DeleteBuiltInAdminRole_IsRejected()
    {
        await using var db = TestDbContextFactory.Create();
        var adminRole = TestEntityFactory.CreateRole(RoleNames.Admin);
        db.Roles.Add(adminRole);
        await db.SaveChangesAsync();
        var service = new RoleService(db, new RecordingPermissionCache());

        await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteRoleAsync(adminRole.RoleId));

        Assert.True(await db.Roles.AnyAsync(role => role.RoleId == adminRole.RoleId));
    }

    [Fact]
    public async Task TC080_RevokePermissionFromBuiltInAdminRole_IsRejected()
    {
        await using var db = TestDbContextFactory.Create();
        var adminRole = TestEntityFactory.CreateRole(RoleNames.Admin);
        var permission = TestEntityFactory.CreatePermission("users.manage");
        var link = LinkPermission(adminRole, permission);
        db.AddRange(adminRole, permission, link);
        await db.SaveChangesAsync();
        var service = new RoleService(db, new RecordingPermissionCache());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.RevokeRolePermissionAsync(adminRole.RoleId, permission.PermissionId));

        Assert.Equal(1, await db.PermissionRoles.CountAsync());
    }

    private static PermissionRole LinkPermission(Role role, Permission permission)
    {
        var link = new PermissionRole
        {
            Id = Guid.NewGuid(),
            RoleId = role.RoleId,
            Role = role,
            PermissionId = permission.PermissionId,
            Permission = permission,
        };
        role.PermissionRoles.Add(link);
        permission.PermissionRoles.Add(link);
        return link;
    }

    private static UserRole LinkUserRole(User user, Role role)
    {
        var link = new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = user.UserId,
            User = user,
            RoleId = role.RoleId,
            Role = role,
        };
        user.UserRoles.Add(link);
        role.UserRoles.Add(link);
        return link;
    }

    private sealed class RecordingPermissionCache : IPermissionCache
    {
        public int InvalidateCallCount { get; private set; }

        public Task<IReadOnlyDictionary<string, IReadOnlySet<string>>> GetPermissionsMapAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyDictionary<string, IReadOnlySet<string>> empty =
                new Dictionary<string, IReadOnlySet<string>>();
            return Task.FromResult(empty);
        }

        public void Invalidate()
        {
            InvalidateCallCount++;
        }
    }
}
