using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.Users;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services.Users;

namespace RoadmapPlatform.Tests;

public sealed class UserAdministrationTests
{
    [Fact]
    public async Task TC066_GetUsers_WithUsernameOrEmailSearch_ReturnsMatchingUsers()
    {
        await using var db = TestDbContextFactory.Create();
        var alice = TestEntityFactory.CreateUser("alice", email: "alice@example.com");
        var bob = TestEntityFactory.CreateUser("bob", email: "bob@sample.com");
        db.AddRange(alice, bob);
        await db.SaveChangesAsync();
        var service = new AdminUserService(db);

        var byUsername = await service.GetUsersAsync("ALI");
        var byEmail = await service.GetUsersAsync("sample.com");

        Assert.Single(byUsername);
        Assert.Equal("alice", byUsername[0].Username);
        Assert.Single(byEmail);
        Assert.Equal("bob", byEmail[0].Username);
    }

    [Fact]
    public async Task TC067_AssignUserRole_WhenNotAssigned_CreatesAssignment()
    {
        await using var db = TestDbContextFactory.Create();
        var user = TestEntityFactory.CreateUser("learner");
        var role = TestEntityFactory.CreateRole("reviewer");
        db.AddRange(user, role);
        await db.SaveChangesAsync();
        var service = new AdminUserService(db);

        var result = await service.AssignUserRoleAsync(user.UserId, role.RoleId);

        Assert.Contains(result.Roles, item => item.RoleId == role.RoleId);
        Assert.Equal(1, await db.UserRoles.CountAsync());
    }

    [Fact]
    public async Task TC068_AssignUserRole_WhenAlreadyAssigned_DoesNotCreateDuplicate()
    {
        await using var db = TestDbContextFactory.Create();
        var user = TestEntityFactory.CreateUser("learner");
        var role = TestEntityFactory.CreateRole("reviewer");
        var assignment = LinkUserRole(user, role);
        db.AddRange(user, role, assignment);
        await db.SaveChangesAsync();
        var service = new AdminUserService(db);

        await service.AssignUserRoleAsync(user.UserId, role.RoleId);

        Assert.Equal(1, await db.UserRoles.CountAsync());
    }

    [Fact]
    public async Task TC069_RevokeUserRole_ForNonAdminRole_RemovesAssignment()
    {
        await using var db = TestDbContextFactory.Create();
        var actor = TestEntityFactory.CreateUser("admin-actor");
        var user = TestEntityFactory.CreateUser("learner");
        var role = TestEntityFactory.CreateRole("reviewer");
        var assignment = LinkUserRole(user, role);
        db.AddRange(actor, user, role, assignment);
        await db.SaveChangesAsync();
        var service = new AdminUserService(db);

        var result = await service.RevokeUserRoleAsync(user.UserId, role.RoleId, actor.UserId);

        Assert.DoesNotContain(result.Roles, item => item.RoleId == role.RoleId);
        Assert.Empty(db.UserRoles);
    }

    [Fact]
    public async Task TC081_RevokeOwnAdminRole_IsRejectedAndAssignmentRemains()
    {
        await using var db = TestDbContextFactory.Create();
        var admin = TestEntityFactory.CreateUser("admin");
        var role = TestEntityFactory.CreateRole(RoleNames.Admin);
        var assignment = LinkUserRole(admin, role);
        db.AddRange(admin, role, assignment);
        await db.SaveChangesAsync();
        var service = new AdminUserService(db);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.RevokeUserRoleAsync(admin.UserId, role.RoleId, admin.UserId));

        Assert.Equal(1, await db.UserRoles.CountAsync());
    }

    private static UserRole LinkUserRole(User user, Role role)
    {
        var assignment = new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = user.UserId,
            User = user,
            RoleId = role.RoleId,
            Role = role,
        };
        user.UserRoles.Add(assignment);
        role.UserRoles.Add(assignment);
        return assignment;
    }
}
