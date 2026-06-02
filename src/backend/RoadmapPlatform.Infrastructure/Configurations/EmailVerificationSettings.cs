namespace RoadmapPlatform.Infrastructure.Configurations
{
    public class EmailVerificationSettings
    {
        public int OtpLength { get; set; } = 6;
        public int ExpirationMinutes { get; set; } = 10;
        public int MaxAttempts { get; set; } = 5;
        public int ResendCooldownSeconds { get; set; } = 60;
        public string HashSecret { get; set; } = string.Empty;
    }
}
