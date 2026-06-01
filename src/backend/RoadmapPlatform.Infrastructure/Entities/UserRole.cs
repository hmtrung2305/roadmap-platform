using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class UserRole
{
    public Guid Id { get; set; }

    public Guid RoleId { get; set; }

    public Guid UserId { get; set; }

    public virtual Role Role { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
