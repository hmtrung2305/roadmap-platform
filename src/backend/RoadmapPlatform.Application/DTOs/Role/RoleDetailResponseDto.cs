using RoadmapPlatform.Application.DTOs.Permissions;
using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.Role
{
    public class RoleDetailResponseDto
    {
        public Guid RoleId { get; set; }

        public string RoleName { get; set; }

        public List<PermissionResponseDto> Permissions { get; set; } = new();
    }
}
