using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.Permissions
{
    public class PermissionResponseDto
    {
        public Guid PermissionId { get; set; }
        public string PermissionName { get; set; } = null!;
    }
}
