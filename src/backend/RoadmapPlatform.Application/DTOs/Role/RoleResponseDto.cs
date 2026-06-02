using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.Role
{
    public class RoleResponseDto
    {
        public Guid RoleId { get; set; }
        [Required]
        public string RoleName { get; set; } = null!;
    }
}
