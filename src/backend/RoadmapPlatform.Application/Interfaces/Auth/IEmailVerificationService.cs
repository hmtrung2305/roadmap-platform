using RoadmapPlatform.Application.DTOs.Auth;
using RoadmapPlatform.Application.DTOs.AuthProviders;

namespace RoadmapPlatform.Application.Interfaces.Auth
{
    public interface IEmailVerificationService
    {
        Task SendVerificationCodeAsync(
            Guid userId,
            string provider,
            string email,
            string purpose,
            CancellationToken cancellationToken = default);

        Task SendPendingRegistrationVerificationCodeAsync(
            Guid pendingLocalRegistrationId,
            string email,
            CancellationToken cancellationToken = default);

        Task VerifyVerificationCodeAsync(
            Guid userId,
            string provider,
            string email,
            string purpose,
            string otp,
            CancellationToken cancellationToken = default);

        Task<PendingRegistrationVerificationResultDto> VerifyRegistrationEmailAsync(
            string email,
            string otp,
            CancellationToken cancellationToken = default);

        Task ResendRegistrationVerificationAsync(
            string email,
            CancellationToken cancellationToken = default);

        Task ResendLinkedLocalVerificationAsync(
            Guid userId,
            CancellationToken cancellationToken);

        Task VerifyLinkedLocalEmailAsync(
            Guid userId,
            string otp,
            CancellationToken cancellationToken = default);

        Task<UpdateLocalEmailResponseDto> RequestLocalEmailChangeAsync(
            Guid userId,
            string newEmail,
            CancellationToken cancellationToken = default);

        Task ResendLocalEmailChangeVerificationAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task VerifyLocalEmailChangeAsync(
            Guid userId,
            string otp,
            CancellationToken cancellationToken = default);
    }
}
