using RoadmapPlatform.Application.DTOs.Auth;
using RoadmapPlatform.Application.DTOs.AuthProviders;

namespace RoadmapPlatform.Application.Interfaces.Auth
{
    /// <summary>
    /// Defines email verification operations for registration, linked local login,
    /// and local email changes.
    /// </summary>
    /// <remarks>
    /// This interface belongs to the Application layer and describes the email
    /// verification use cases supported by the system.
    ///
    /// Implementations are responsible for generating OTP codes, storing verification
    /// records, sending verification emails, validating OTP codes, and updating the
    /// related account or authentication provider state.
    /// </remarks>
    public interface IEmailVerificationService
    {
        /// <summary>
        /// Sends a verification code for a specific user authentication provider email.
        /// </summary>
        /// <param name="userId">
        /// The user ID that owns the authentication provider.
        /// </param>
        /// <param name="provider">
        /// The authentication provider name.
        /// </param>
        /// <param name="email">
        /// The email address that should receive the verification code.
        /// </param>
        /// <param name="purpose">
        /// The verification purpose.
        /// </param>
        /// <param name="cancellationToken">
        /// A token used to cancel the operation.
        /// </param>
        Task SendVerificationCodeAsync(
            Guid userId,
            string provider,
            string email,
            string purpose,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a verification code for a pending local registration.
        /// </summary>
        /// <param name="pendingLocalRegistrationId">
        /// The pending local registration ID.
        /// </param>
        /// <param name="email">
        /// The email address that should receive the verification code.
        /// </param>
        /// <param name="cancellationToken">
        /// A token used to cancel the operation.
        /// </param>
        /// <remarks>
        /// This method is used before the final user account is created.
        /// </remarks>
        Task SendPendingRegistrationVerificationCodeAsync(
            Guid pendingLocalRegistrationId,
            string email,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifies an OTP code for a specific user authentication provider email.
        /// </summary>
        /// <param name="userId">
        /// The user ID that owns the authentication provider.
        /// </param>
        /// <param name="provider">
        /// The authentication provider name.
        /// </param>
        /// <param name="email">
        /// The email address being verified.
        /// </param>
        /// <param name="purpose">
        /// The verification purpose.
        /// </param>
        /// <param name="otp">
        /// The OTP code submitted by the user.
        /// </param>
        /// <param name="cancellationToken">
        /// A token used to cancel the operation.
        /// </param>
        Task VerifyVerificationCodeAsync(
            Guid userId,
            string provider,
            string email,
            string purpose,
            string otp,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifies a pending registration email and returns the pending registration data.
        /// </summary>
        /// <param name="email">
        /// The email address being verified.
        /// </param>
        /// <param name="otp">
        /// The OTP code submitted by the user.
        /// </param>
        /// <param name="cancellationToken">
        /// A token used to cancel the operation.
        /// </param>
        /// <returns>
        /// The verified pending registration data needed to create the final local account.
        /// </returns>
        Task<PendingRegistrationVerificationResultDto> VerifyRegistrationEmailAsync(
            string email,
            string otp,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Resends the verification code for a pending local registration.
        /// </summary>
        /// <param name="email">
        /// The email address associated with the pending registration.
        /// </param>
        /// <param name="cancellationToken">
        /// A token used to cancel the operation.
        /// </param>
        Task ResendRegistrationVerificationAsync(
            string email,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Resends the verification code for a linked local authentication provider.
        /// </summary>
        /// <param name="userId">
        /// The user ID that owns the linked local authentication provider.
        /// </param>
        /// <param name="cancellationToken">
        /// A token used to cancel the operation.
        /// </param>
        Task ResendLinkedLocalVerificationAsync(
            Guid userId,
            CancellationToken cancellationToken);

        /// <summary>
        /// Verifies the email for a linked local authentication provider.
        /// </summary>
        /// <param name="userId">
        /// The user ID that owns the linked local authentication provider.
        /// </param>
        /// <param name="otp">
        /// The OTP code submitted by the user.
        /// </param>
        /// <param name="cancellationToken">
        /// A token used to cancel the operation.
        /// </param>
        Task VerifyLinkedLocalEmailAsync(
            Guid userId,
            string otp,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Requests a local account email change and sends a verification code
        /// to the new email address.
        /// </summary>
        /// <param name="userId">
        /// The user ID requesting the email change.
        /// </param>
        /// <param name="newEmail">
        /// The new email address to verify.
        /// </param>
        /// <param name="cancellationToken">
        /// A token used to cancel the operation.
        /// </param>
        /// <returns>
        /// A response describing the email change verification state.
        /// </returns>
        Task<UpdateLocalEmailResponseDto> RequestLocalEmailChangeAsync(
            Guid userId,
            string newEmail,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Resends the verification code for a pending local email change.
        /// </summary>
        /// <param name="userId">
        /// The user ID that requested the email change.
        /// </param>
        /// <param name="cancellationToken">
        /// A token used to cancel the operation.
        /// </param>
        Task ResendLocalEmailChangeVerificationAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifies a pending local email change.
        /// </summary>
        /// <param name="userId">
        /// The user ID that requested the email change.
        /// </param>
        /// <param name="otp">
        /// The OTP code submitted by the user.
        /// </param>
        /// <param name="cancellationToken">
        /// A token used to cancel the operation.
        /// </param>
        Task VerifyLocalEmailChangeAsync(
            Guid userId,
            string otp,
            CancellationToken cancellationToken = default);
    }
}