using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class Permission
{
    public Guid PermissionId { get; set; }

    public string PermissionName { get; set; } = null!;

    public virtual ICollection<PermissionRole> PermissionRoles { get; set; } = new List<PermissionRole>();
}
