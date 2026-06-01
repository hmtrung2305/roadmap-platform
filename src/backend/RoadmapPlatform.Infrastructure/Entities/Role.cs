using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class Role
{
    public Guid RoleId { get; set; }

    public string RoleName { get; set; } = null!;

    public virtual ICollection<PermissionRole> PermissionRoles { get; set; } = new List<PermissionRole>();

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
