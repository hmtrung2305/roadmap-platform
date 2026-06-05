namespace RoadmapPlatform.Application.DTOs.AiCredits
{
    public class AiCreditStatusDto
    {
        public string PlanCode { get; set; } = "free";

        public int DailyCreditLimit { get; set; }

        public int UsedCreditsToday { get; set; }

        public int RemainingCreditsToday { get; set; }

        public DateTimeOffset ResetAt { get; set; }
    }
}