using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Tests;

internal static class TestEntityFactory
{
    public static User CreateUser(
        string username,
        string status = "active",
        string? email = null,
        Guid? userId = null)
    {
        var now = DateTime.UtcNow;
        var user = new User
        {
            UserId = userId ?? Guid.NewGuid(),
            Username = username,
            UsernameNormalized = username.Trim().ToUpperInvariant(),
            Status = status,
            CreatedAt = now,
            UpdatedAt = now,
        };

        if (email is not null)
        {
            var provider = new UserAuthProvider
            {
                Id = Guid.NewGuid(),
                UserId = user.UserId,
                User = user,
                Email = email,
                Provider = "local",
                ProviderUserId = user.UserId.ToString("N"),
                CreatedAt = now,
                EmailVerifiedAt = now,
            };
            user.UserAuthProviders.Add(provider);
        }

        return user;
    }

    public static Role CreateRole(string name, Guid? roleId = null)
    {
        return new Role
        {
            RoleId = roleId ?? Guid.NewGuid(),
            RoleName = name,
        };
    }

    public static Permission CreatePermission(string name, Guid? permissionId = null)
    {
        return new Permission
        {
            PermissionId = permissionId ?? Guid.NewGuid(),
            PermissionName = name,
        };
    }

    public static Skill CreateSkill(
        string name,
        string slug,
        string category = "Programming",
        Guid? skillId = null)
    {
        var now = DateTime.UtcNow;
        return new Skill
        {
            SkillId = skillId ?? Guid.NewGuid(),
            Name = name,
            Slug = slug,
            Category = category,
            Description = $"{name} description",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public static LearningResource CreateLearningResource(
        string title,
        string url,
        string difficulty = "beginner",
        Guid? resourceId = null)
    {
        var now = DateTime.UtcNow;
        return new LearningResource
        {
            LearningResourceId = resourceId ?? Guid.NewGuid(),
            Title = title,
            Url = url,
            ResourceType = "course",
            Description = $"{title} description",
            Provider = "Roadmap Platform",
            DifficultyLevel = difficulty,
            LanguageCode = "en",
            VerificationStatus = "verified",
            CreatedAt = now,
            UpdatedAt = now,
        };
    }
}
