using RoadmapPlatform.Application.Interfaces;

namespace RoadmapPlatform.Infrastructure.Services.Email
{
    public class ConsoleEmailSender : IEmailSender
    {
        public Task SendEmailAsync(string to, string subject, string body)
        {
            Console.WriteLine("========== EMAIL ==========");
            Console.WriteLine($"To: {to}");
            Console.WriteLine($"Subject: {subject}");
            Console.WriteLine(body);
            Console.WriteLine("===========================");

            return Task.CompletedTask;
        }
    }
}
