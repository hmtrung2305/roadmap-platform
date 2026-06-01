using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class PermissionRole
{
    public Guid Id { get; set; }

    public Guid PermissionId { get; set; }

    public Guid RoleId { get; set; }

    public virtual Permission Permission { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;
}
