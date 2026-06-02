using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.PermissionRole
{
    public class AssignPermissionRoleRequestDto
    {
        public List<Guid> PermissionIds { get; set; } = new();
    }
}
