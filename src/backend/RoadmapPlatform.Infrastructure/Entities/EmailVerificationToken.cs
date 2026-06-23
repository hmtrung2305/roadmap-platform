using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class EmailVerificationToken
{
    public Guid VerificationId { get; set; }

    public Guid? UserId { get; set; }

    public Guid? PendingLocalRegistrationId { get; set; }

    public string Provider { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Purpose { get; set; } = null!;

    public string OtpHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public int AttemptCount { get; set; }

    public int MaxAttempts { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual PendingLocalRegistration? PendingLocalRegistration { get; set; }

    public virtual User? User { get; set; }
}
