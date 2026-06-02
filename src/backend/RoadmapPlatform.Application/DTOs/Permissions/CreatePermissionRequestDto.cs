using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.Permissions
{
    public class CreatePermissionRequestDto
    {
        [Required]
        public string PermissionName { get; set; } = null!;
    }
}
