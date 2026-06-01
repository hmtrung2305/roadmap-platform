using System.ComponentModel.DataAnnotations;

namespace RoadmapPlatform.Application.DTOs.Users;

public class UpdateProfileRequestDto
{
    [StringLength(50, ErrorMessage = "Display name cannot exceed 50 characters")]
    public string? DisplayName { get; set; }
    [StringLength(150, ErrorMessage = "Headline cannot exceed 150 characters")]
    public string? Headline { get; set; }
    [StringLength(500, ErrorMessage = "Bio cannot exceed 500 characters")]
    public string? Bio { get; set; }
    public string? Location { get; set; }
    [Url(ErrorMessage = "Avatar URL must be a valid URL")]
    public string? AvatarUrl { get; set; }
    [Url(ErrorMessage = "Cover image URL must be a valid URL")]
    public string? CoverImageUrl { get; set; }
    public string? CareerGoal { get; set; }
    public string? CurrentRole { get; set; }
    [EmailAddress]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        ErrorMessage = "Invalid email format.")]
    public string? PublicEmail { get; set; }

    [Url(ErrorMessage = "GitHub URL must be a valid URL")]
    public string? GithubUrl { get; set; }

    [Url(ErrorMessage = "LinkedIn URL must be a valid URL")]
    public string? LinkedinUrl { get; set; }

    [Url(ErrorMessage = "Personal website URL must be a valid URL")]
    public string? PersonalWebsiteUrl { get; set; }
    public bool? IsPublic { get; set; }
}