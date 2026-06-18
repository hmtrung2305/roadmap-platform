using Microsoft.AspNetCore.Authorization;
using RoadmapPlatform.Application.Constants;

namespace RoadmapPlatform.Api.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
    {
        Policy = PermissionPolicyNames.For(permission);
    }
}
