using Microsoft.AspNetCore.Authorization;
using RoadmapPlatform.Application.Constants;

namespace RoadmapPlatform.Api.Authorization;

/// <summary>
/// Requires the current authenticated user to have a specific permission.
/// </summary>
/// <remarks>
/// This attribute converts a permission code into an ASP.NET Core authorization policy name.
/// The actual permission validation is handled by the permission policy provider and handler.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequirePermissionAttribute"/> class.
    /// </summary>
    /// <param name="permission">The permission code required to access the endpoint.</param>
    public RequirePermissionAttribute(string permission)
    {
        Policy = PermissionPolicyNames.For(permission);
    }
}

/// <summary>
/// Requires the current authenticated user to have at least one of the specified permissions.
/// </summary>
/// <remarks>
/// This attribute is useful for endpoints that can be accessed by multiple permission groups.
/// The actual permission validation is handled by the permission policy provider and handler.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequireAnyPermissionAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequireAnyPermissionAttribute"/> class.
    /// </summary>
    /// <param name="permissions">The accepted permission codes. The user must have at least one of them.</param>
    public RequireAnyPermissionAttribute(params string[] permissions)
    {
        Policy = PermissionPolicyNames.ForAny(permissions);
    }
}
