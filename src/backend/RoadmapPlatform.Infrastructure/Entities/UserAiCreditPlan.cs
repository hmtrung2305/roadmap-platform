namespace RoadmapPlatform.Infrastructure.Entities;

public partial class UserAiCreditPlan
{
    public Guid UserId { get; set; }

    public string PlanCode { get; set; } = null!;

    public DateTimeOffset? ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public virtual AiCreditPlan Plan { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}