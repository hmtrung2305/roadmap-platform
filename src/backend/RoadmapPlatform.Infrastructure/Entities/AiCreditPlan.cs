namespace RoadmapPlatform.Infrastructure.Entities;

public partial class AiCreditPlan
{
    public string PlanCode { get; set; } = null!;

    public int DailyCreditLimit { get; set; }

    public int? MonthlyCreditLimit { get; set; }

    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public virtual ICollection<UserAiCreditPlan> UserAiCreditPlans { get; set; } = new List<UserAiCreditPlan>();
}