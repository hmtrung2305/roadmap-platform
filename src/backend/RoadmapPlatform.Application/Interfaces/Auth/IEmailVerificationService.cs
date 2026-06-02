using RoadmapPlatform.Application.DTOs.Auth;

namespace RoadmapPlatform.Application.Interfaces.Auth
{
    public interface IEmailVerificationService
    {
        Task SendVerificationCodeAsync(
        Guid userId,
        string provider,
        string email,
        string purpose);

        Task VerifyVerificationCodeAsync(
            Guid userId,
            string provider,
            string email,
            string purpose,
            string otp);

        Task<EmailVerificationResultDto> VerifyRegistrationEmailAsync(string email, string otp);

        Task ResendRegistrationVerificationAsync(string email);

        Task ResendLinkedLocalVerificationAsync(Guid userId);

        Task VerifyLinkedLocalEmailAsync(Guid userId, string otp);

        Task RequestLocalEmailChangeAsync(Guid userId, string newEmail);

        Task VerifyLocalEmailChangeAsync(Guid userId, string otp);
    }
}
