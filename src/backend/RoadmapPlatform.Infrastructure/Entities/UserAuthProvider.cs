using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class UserAuthProvider
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string? Email { get; set; }

    public string? PasswordHash { get; set; }

    public string Provider { get; set; } = null!;

    public string ProviderUserId { get; set; } = null!;

    public string? ProviderUsername { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? PendingEmail { get; set; }

    public DateTime? EmailVerifiedAt { get; set; }

    public string? AccessToken { get; set; }
    
    public virtual User User { get; set; } = null!;
}
