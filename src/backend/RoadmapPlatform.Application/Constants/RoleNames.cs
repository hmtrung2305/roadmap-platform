namespace RoadmapPlatform.Application.Constants;

/// <summary>
/// Defines the role names used by the authorization system.
/// </summary>
public static class RoleNames
{
    /// <summary>
    /// Represents the default learner role for regular users.
    /// </summary>
    public const string Learner = "learner";

    /// <summary>
    /// Represents the role responsible for managing learning content.
    /// </summary>
    public const string ContentManager = "content_manager";

    /// <summary>
    /// Represents the role responsible for reviewing and approving content changes.
    /// </summary>
    public const string Reviewer = "reviewer";

    /// <summary>
    /// Represents the administrator role with system-level permissions.
    /// </summary>
    public const string Admin = "admin";
}
