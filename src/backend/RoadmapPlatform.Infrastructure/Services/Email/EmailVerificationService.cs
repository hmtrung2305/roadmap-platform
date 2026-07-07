using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.Auth;
using RoadmapPlatform.Application.DTOs.AuthProviders;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces;
using RoadmapPlatform.Application.Interfaces.Auth;
using RoadmapPlatform.Infrastructure.Configurations;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services.Email.Templates;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace RoadmapPlatform.Infrastructure.Services.Email
{
    /// <summary>
    /// Implements email verification flows for registration, linked local login,
    /// and local email change.
    /// </summary>
    /// <remarks>
    /// This service creates OTP codes, stores hashed verification tokens,
    /// sends verification emails, validates submitted OTP codes, and updates
    /// email verification state in the database.
    ///
    /// OTP values are never stored as plain text. They are hashed using a
    /// configured secret before being saved.
    /// </remarks>
    public class EmailVerificationService : IEmailVerificationService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IEmailSender _emailSender;
        private readonly EmailVerificationSettings _settings;

        /// <summary>
        /// Creates a new email verification service.
        /// </summary>
        public EmailVerificationService(
            ApplicationDbContext dbContext,
            IEmailSender emailSender,
            IOptions<EmailVerificationSettings> options)
        {
            _dbContext = dbContext;
            _emailSender = emailSender;
            _settings = options.Value;
        }

        /// <summary>
        /// Sends a verification code for an existing user authentication provider email.
        /// </summary>
        /// <remarks>
        /// This method is used when the user account already exists.
        /// It validates the target user, enforces resend cooldown, invalidates old
        /// active tokens, creates a new OTP token, and sends the OTP by email.
        /// </remarks>
        public async Task SendVerificationCodeAsync(
            Guid userId,
            string provider,
            string email,
            string purpose,
            CancellationToken cancellationToken = default)
        {
            provider = NormalizeOrThrow(provider, "Provider was not provided");
            email = NormalizeEmailOrThrow(email);
            purpose = NormalizeOrThrow(purpose, "Purpose was not provided");

            var userExists = await _dbContext.Users
                .AnyAsync(x => x.UserId == userId, cancellationToken);

            if (!userExists)
            {
                throw new NotFoundException("User was not found");
            }

            await EnsureResendCooldownHasPassedAsync(
                userId,
                provider,
                email,
                purpose,
                cancellationToken);

            await InvalidateExistingTokensAsync(
                userId,
                provider,
                email,
                purpose,
                cancellationToken);

            var otp = GenerateOtp(_settings.OtpLength);
            var otpHash = HashOtp(otp);
            var now = DateTime.UtcNow;

            var token = new EmailVerificationToken
            {
                UserId = userId,
                Provider = provider,
                Email = email,
                Purpose = purpose,
                OtpHash = otpHash,
                ExpiresAt = now.AddMinutes(_settings.ExpirationMinutes),
                AttemptCount = 0,
                MaxAttempts = _settings.MaxAttempts,
                CreatedAt = now
            };

            _dbContext.EmailVerificationTokens.Add(token);

            await _dbContext.SaveChangesAsync(cancellationToken);

            var htmlBody = EmailVerificationTemplate.Build(
                otp,
                _settings.ExpirationMinutes);

            await _emailSender.SendEmailAsync(
                email,
                "Verify your Roadmap Platform email",
                htmlBody,
                cancellationToken);
        }

        /// <summary>
        /// Verifies an OTP code for an existing user authentication provider email.
        /// </summary>
        /// <remarks>
        /// This method validates the latest unused token for the specified
        /// user, provider, email, and purpose.
        ///
        /// Invalid attempts increase AttemptCount. A valid OTP marks the token as used.
        /// </remarks>
        public async Task VerifyVerificationCodeAsync(
            Guid userId,
            string provider,
            string email,
            string purpose,
            string otp,
            CancellationToken cancellationToken = default)
        {
            provider = NormalizeOrThrow(provider, "Provider was not provided");
            email = NormalizeEmailOrThrow(email);
            purpose = NormalizeOrThrow(purpose, "Purpose was not provided");
            otp = NormalizeOrThrow(otp, "OTP was not provided");

            var token = await GetLatestUsableTokenAsync(
                userId,
                provider,
                email,
                purpose,
                cancellationToken);

            if (token.ExpiresAt <= DateTime.UtcNow)
            {
                throw new InvalidOperationException("Verification code has expired");
            }

            if (token.AttemptCount >= token.MaxAttempts)
            {
                throw new InvalidOperationException("Too many invalid attempts");
            }

            var providedOtpHash = HashOtp(otp);

            if (!FixedTimeEquals(token.OtpHash, providedOtpHash))
            {
                token.AttemptCount++;

                await _dbContext.SaveChangesAsync(cancellationToken);

                throw new InvalidOperationException("Invalid verification code");
            }

            token.UsedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Sends a verification code for a pending local registration.
        /// </summary>
        /// <remarks>
        /// This method is used before the final User record is created.
        /// The token is linked to PendingLocalRegistrationId instead of UserId.
        /// </remarks>
        public async Task SendPendingRegistrationVerificationCodeAsync(
            Guid pendingLocalRegistrationId,
            string email,
            CancellationToken cancellationToken = default)
        {
            email = NormalizeEmailOrThrow(email);

            var pendingRegistrationExists = await _dbContext.PendingLocalRegistrations
                .AnyAsync(
                    x => x.PendingLocalRegistrationId == pendingLocalRegistrationId &&
                         x.Email == email &&
                         x.UsedAt == null,
                    cancellationToken);

            if (!pendingRegistrationExists)
            {
                throw new NotFoundException("Pending registration was not found");
            }

            await EnsurePendingRegistrationResendCooldownHasPassedAsync(
                pendingLocalRegistrationId,
                email,
                cancellationToken);

            await InvalidateExistingPendingRegistrationTokensAsync(
                pendingLocalRegistrationId,
                email,
                cancellationToken);

            var otp = GenerateOtp(_settings.OtpLength);
            var otpHash = HashOtp(otp);
            var now = DateTime.UtcNow;

            var token = new EmailVerificationToken
            {
                UserId = null,
                PendingLocalRegistrationId = pendingLocalRegistrationId,
                Provider = AuthProviders.Local,
                Email = email,
                Purpose = EmailVerificationPurposes.Register,
                OtpHash = otpHash,
                ExpiresAt = now.AddMinutes(_settings.ExpirationMinutes),
                AttemptCount = 0,
                MaxAttempts = _settings.MaxAttempts,
                CreatedAt = now
            };

            _dbContext.EmailVerificationTokens.Add(token);

            await _dbContext.SaveChangesAsync(cancellationToken);

            var htmlBody = EmailVerificationTemplate.Build(
                otp,
                _settings.ExpirationMinutes);

            await _emailSender.SendEmailAsync(
                email,
                "Verify your Roadmap Platform email",
                htmlBody,
                cancellationToken);
        }

        /// <summary>
        /// Verifies a pending registration email and returns the pending account data.
        /// </summary>
        /// <remarks>
        /// This method does not create the final user.
        /// It only verifies the OTP and returns the pending registration data
        /// needed by AuthService to create the real user account.
        /// </remarks>
        public async Task<PendingRegistrationVerificationResultDto> VerifyRegistrationEmailAsync(
            string email,
            string otp,
            CancellationToken cancellationToken = default)
        {
            email = NormalizeEmailOrThrow(email);
            otp = NormalizeOrThrow(otp, "OTP was not provided");

            var pendingRegistration = await _dbContext.PendingLocalRegistrations
                .FirstOrDefaultAsync(
                    x => x.Email == email &&
                         x.UsedAt == null,
                    cancellationToken);

            if (pendingRegistration == null)
            {
                throw new NotFoundException("Pending registration was not found");
            }

            if (pendingRegistration.ExpiresAt <= DateTime.UtcNow)
            {
                throw new InvalidOperationException("Pending registration has expired. Please register again.");
            }

            await VerifyPendingRegistrationCodeAsync(
                pendingRegistration.PendingLocalRegistrationId,
                email,
                otp,
                cancellationToken);

            return new PendingRegistrationVerificationResultDto
            {
                PendingLocalRegistrationId = pendingRegistration.PendingLocalRegistrationId,
                Username = pendingRegistration.Username,
                UsernameNormalized = pendingRegistration.UsernameNormalized,
                Email = pendingRegistration.Email,
                PasswordHash = pendingRegistration.PasswordHash
            };
        }

        /// <summary>
        /// Resends the verification code for an active pending registration.
        /// </summary>
        public async Task ResendRegistrationVerificationAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            email = NormalizeEmailOrThrow(email);

            var pendingRegistration = await _dbContext.PendingLocalRegistrations
                .FirstOrDefaultAsync(
                    x => x.Email == email &&
                         x.UsedAt == null,
                    cancellationToken);

            if (pendingRegistration == null)
            {
                throw new NotFoundException("Pending registration was not found");
            }

            if (pendingRegistration.ExpiresAt <= DateTime.UtcNow)
            {
                throw new InvalidOperationException("Pending registration has expired. Please register again.");
            }

            await SendPendingRegistrationVerificationCodeAsync(
                pendingRegistration.PendingLocalRegistrationId,
                email,
                cancellationToken);
        }

        /// <summary>
        /// Resends the verification code for a linked local login method.
        /// </summary>
        /// <remarks>
        /// This is used when a user has a local auth provider that exists
        /// but its email has not been verified yet.
        /// </remarks>
        public async Task ResendLinkedLocalVerificationAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var localProvider = await _dbContext.UserAuthProviders
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.Provider == AuthProviders.Local,
                    cancellationToken);

            if (localProvider == null)
            {
                throw new NotFoundException("Local login method was not found");
            }

            if (string.IsNullOrWhiteSpace(localProvider.Email))
            {
                throw new InvalidOperationException("Local email was not found");
            }

            if (localProvider.EmailVerifiedAt != null)
            {
                throw new InvalidOperationException("Local email is already verified");
            }

            await SendVerificationCodeAsync(
                userId,
                AuthProviders.Local,
                localProvider.Email,
                EmailVerificationPurposes.LinkLocal,
                cancellationToken);
        }

        /// <summary>
        /// Verifies the email for a linked local login method.
        /// </summary>
        public async Task VerifyLinkedLocalEmailAsync(
            Guid userId,
            string otp,
            CancellationToken cancellationToken = default)
        {
            otp = NormalizeOrThrow(otp, "OTP was not provided");

            var localProvider = await _dbContext.UserAuthProviders
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.Provider == AuthProviders.Local,
                    cancellationToken);

            if (localProvider == null)
            {
                throw new NotFoundException("Local login method was not found");
            }

            if (string.IsNullOrWhiteSpace(localProvider.Email))
            {
                throw new InvalidOperationException("Local email was not found");
            }

            if (localProvider.EmailVerifiedAt != null)
            {
                throw new InvalidOperationException("Local email is already verified");
            }

            await VerifyVerificationCodeAsync(
                userId,
                AuthProviders.Local,
                localProvider.Email,
                EmailVerificationPurposes.LinkLocal,
                otp,
                cancellationToken);

            localProvider.EmailVerifiedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Starts a local email change flow and sends a verification code to the new email.
        /// </summary>
        public async Task<UpdateLocalEmailResponseDto> RequestLocalEmailChangeAsync(
            Guid userId,
            string newEmail,
            CancellationToken cancellationToken = default)
        {
            newEmail = NormalizeEmailOrThrow(newEmail, "New email was not provided");

            var localProvider = await _dbContext.UserAuthProviders
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.Provider == AuthProviders.Local,
                    cancellationToken);

            if (localProvider == null)
            {
                throw new NotFoundException("Local login method was not found");
            }

            if (string.IsNullOrWhiteSpace(localProvider.PasswordHash))
            {
                throw new InvalidOperationException("This account does not have a local password login");
            }

            if (localProvider.ProviderUserId == newEmail)
            {
                throw new InvalidOperationException("New email must be different from the current email");
            }

            var emailExists = await _dbContext.UserAuthProviders
                .AnyAsync(x =>
                    x.Provider == AuthProviders.Local &&
                    x.ProviderUserId == newEmail &&
                    x.UserId != userId,
                    cancellationToken);

            if (emailExists)
            {
                throw new ConflictException("Email is already registered");
            }

            var pendingEmail = localProvider.PendingEmail?.Trim().ToLowerInvariant();

            if (pendingEmail == newEmail)
            {
                return CreateChangeEmailVerificationResponse(
                    newEmail,
                    "Email change is already pending. Please verify your new email to continue.");
            }

            localProvider.PendingEmail = newEmail;

            await _dbContext.SaveChangesAsync(cancellationToken);

            await SendVerificationCodeAsync(
                userId,
                AuthProviders.Local,
                newEmail,
                EmailVerificationPurposes.ChangeEmail,
                cancellationToken);

            return CreateChangeEmailVerificationResponse(
                newEmail,
                "Verification code sent");
        }

        /// <summary>
        /// Resends the verification code for a pending local email change.
        /// </summary>
        public async Task ResendLocalEmailChangeVerificationAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var localProvider = await _dbContext.UserAuthProviders
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.Provider == AuthProviders.Local,
                    cancellationToken);

            if (localProvider == null)
            {
                throw new NotFoundException("Local login method was not found");
            }

            if (string.IsNullOrWhiteSpace(localProvider.PasswordHash))
            {
                throw new InvalidOperationException("This account does not have a local password login");
            }

            var pendingEmail = localProvider.PendingEmail?.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(pendingEmail))
            {
                throw new InvalidOperationException("No pending email change was found");
            }

            var emailExists = await _dbContext.UserAuthProviders
                .AnyAsync(x =>
                    x.Provider == AuthProviders.Local &&
                    x.ProviderUserId == pendingEmail &&
                    x.UserId != userId,
                    cancellationToken);

            if (emailExists)
            {
                throw new ConflictException("Email is already registered");
            }

            await SendVerificationCodeAsync(
                userId,
                AuthProviders.Local,
                pendingEmail,
                EmailVerificationPurposes.ChangeEmail,
                cancellationToken);
        }

        /// <summary>
        /// Verifies a pending local email change and applies the new email.
        /// </summary>
        public async Task VerifyLocalEmailChangeAsync(
            Guid userId,
            string otp,
            CancellationToken cancellationToken = default)
        {
            otp = NormalizeOrThrow(otp, "OTP was not provided");

            var localProvider = await _dbContext.UserAuthProviders
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.Provider == AuthProviders.Local,
                    cancellationToken);

            if (localProvider == null)
            {
                throw new NotFoundException("Local login method was not found");
            }

            var pendingEmail = localProvider.PendingEmail?.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(pendingEmail))
            {
                throw new InvalidOperationException("No pending email change was found");
            }

            var emailExists = await _dbContext.UserAuthProviders
                .AnyAsync(x =>
                    x.Provider == AuthProviders.Local &&
                    x.ProviderUserId == pendingEmail &&
                    x.UserId != userId,
                    cancellationToken);

            if (emailExists)
            {
                throw new ConflictException("Email is already registered");
            }

            await VerifyVerificationCodeAsync(
                userId,
                AuthProviders.Local,
                pendingEmail,
                EmailVerificationPurposes.ChangeEmail,
                otp,
                cancellationToken);

            localProvider.Email = pendingEmail;
            localProvider.ProviderUserId = pendingEmail;
            localProvider.PendingEmail = null;
            localProvider.EmailVerifiedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Creates a standard response for email-change verification flows.
        /// </summary>
        private static UpdateLocalEmailResponseDto CreateChangeEmailVerificationResponse(
            string email,
            string message)
        {
            return new UpdateLocalEmailResponseDto
            {
                Message = message,
                Email = email,
                RequiresEmailVerification = true,
                VerificationPurpose = EmailVerificationPurposes.ChangeEmail,
                CanResendVerification = true
            };
        }

        /// <summary>
        /// Generates a numeric OTP with the configured length.
        /// </summary>
        /// <remarks>
        /// The OTP is generated using RandomNumberGenerator for cryptographic randomness.
        /// </remarks>
        private static string GenerateOtp(int length)
        {
            if (length <= 0)
            {
                throw new InvalidOperationException("OTP length must be greater than zero");
            }

            var min = (int)Math.Pow(10, length - 1);
            var max = (int)Math.Pow(10, length);

            return RandomNumberGenerator.GetInt32(min, max).ToString();
        }

        /// <summary>
        /// Hashes an OTP using HMAC-SHA256 and the configured hash secret.
        /// </summary>
        private string HashOtp(string otp)
        {
            if (string.IsNullOrWhiteSpace(_settings.HashSecret))
            {
                throw new InvalidOperationException("Email verification hash secret was not found");
            }

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.HashSecret));

            var bytes = Encoding.UTF8.GetBytes(otp);
            var hashBytes = hmac.ComputeHash(bytes);

            return Convert.ToHexString(hashBytes);
        }

        /// <summary>
        /// Ensures that a pending registration is not requesting OTP resend too frequently.
        /// </summary>
        private async Task EnsurePendingRegistrationResendCooldownHasPassedAsync(
            Guid pendingLocalRegistrationId,
            string email,
            CancellationToken cancellationToken = default)
        {
            var latestToken = await _dbContext.EmailVerificationTokens
                .Where(x =>
                    x.PendingLocalRegistrationId == pendingLocalRegistrationId &&
                    x.Provider == AuthProviders.Local &&
                    x.Email == email &&
                    x.Purpose == EmailVerificationPurposes.Register)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (latestToken == null)
            {
                return;
            }

            var nextAllowedAt = latestToken.CreatedAt.AddSeconds(_settings.ResendCooldownSeconds);

            if (DateTime.UtcNow < nextAllowedAt)
            {
                var remainingSeconds = (int)Math.Ceiling(
                    (nextAllowedAt - DateTime.UtcNow).TotalSeconds);

                throw new InvalidOperationException(
                    $"Please wait {remainingSeconds} seconds before requesting another code");
            }
        }

        /// <summary>
        /// Marks all active pending-registration tokens as used before creating a new one.
        /// </summary>
        private async Task InvalidateExistingPendingRegistrationTokensAsync(
            Guid pendingLocalRegistrationId,
            string email,
            CancellationToken cancellationToken = default)
        {
            var existingTokens = await _dbContext.EmailVerificationTokens
                .Where(x =>
                    x.PendingLocalRegistrationId == pendingLocalRegistrationId &&
                    x.Provider == AuthProviders.Local &&
                    x.Email == email &&
                    x.Purpose == EmailVerificationPurposes.Register &&
                    x.UsedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var token in existingTokens)
            {
                token.UsedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Verifies the latest active OTP token for a pending registration.
        /// </summary>
        private async Task VerifyPendingRegistrationCodeAsync(
            Guid pendingLocalRegistrationId,
            string email,
            string otp,
            CancellationToken cancellationToken = default)
        {
            var token = await _dbContext.EmailVerificationTokens
                .Where(x =>
                    x.PendingLocalRegistrationId == pendingLocalRegistrationId &&
                    x.Provider == AuthProviders.Local &&
                    x.Email == email &&
                    x.Purpose == EmailVerificationPurposes.Register &&
                    x.UsedAt == null)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (token == null)
            {
                throw new NotFoundException("Verification code was not found");
            }

            if (token.ExpiresAt <= DateTime.UtcNow)
            {
                throw new InvalidOperationException("Verification code has expired");
            }

            if (token.AttemptCount >= token.MaxAttempts)
            {
                throw new InvalidOperationException("Too many invalid attempts");
            }

            var providedOtpHash = HashOtp(otp);

            if (!FixedTimeEquals(token.OtpHash, providedOtpHash))
            {
                token.AttemptCount++;

                await _dbContext.SaveChangesAsync(cancellationToken);

                throw new InvalidOperationException("Invalid verification code");
            }

            token.UsedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Gets the latest unused verification token for an existing user flow.
        /// </summary>
        private async Task<EmailVerificationToken> GetLatestUsableTokenAsync(
            Guid userId,
            string provider,
            string email,
            string purpose,
            CancellationToken cancellationToken = default)
        {
            var token = await _dbContext.EmailVerificationTokens
                .Where(x =>
                    x.UserId == userId &&
                    x.Provider == provider &&
                    x.Email == email &&
                    x.Purpose == purpose &&
                    x.UsedAt == null)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (token == null)
            {
                throw new NotFoundException("Verification code was not found");
            }

            return token;
        }

        /// <summary>
        /// Ensures that an existing user flow is not requesting OTP resend too frequently.
        /// </summary>
        private async Task EnsureResendCooldownHasPassedAsync(
            Guid userId,
            string provider,
            string email,
            string purpose,
            CancellationToken cancellationToken = default)
        {
            var latestToken = await _dbContext.EmailVerificationTokens
                .Where(x =>
                    x.UserId == userId &&
                    x.Provider == provider &&
                    x.Email == email &&
                    x.Purpose == purpose)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (latestToken == null)
            {
                return;
            }

            var nextAllowedAt = latestToken.CreatedAt.AddSeconds(_settings.ResendCooldownSeconds);

            if (DateTime.UtcNow < nextAllowedAt)
            {
                var remainingSeconds = (int)Math.Ceiling(
                    (nextAllowedAt - DateTime.UtcNow).TotalSeconds);

                throw new InvalidOperationException(
                    $"Please wait {remainingSeconds} seconds before requesting another code");
            }
        }

        /// <summary>
        /// Marks all active verification tokens for an existing user flow as used.
        /// </summary>
        private async Task InvalidateExistingTokensAsync(
            Guid userId,
            string provider,
            string email,
            string purpose,
            CancellationToken cancellationToken = default)
        {
            var existingTokens = await _dbContext.EmailVerificationTokens
                .Where(x =>
                    x.UserId == userId &&
                    x.Provider == provider &&
                    x.Email == email &&
                    x.Purpose == purpose &&
                    x.UsedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var token in existingTokens)
            {
                token.UsedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Compares two strings using fixed-time comparison.
        /// </summary>
        /// <remarks>
        /// This helps reduce timing attacks when comparing OTP hashes.
        /// </remarks>
        private static bool FixedTimeEquals(string valueA, string valueB)
        {
            var bytesA = Encoding.UTF8.GetBytes(valueA);
            var bytesB = Encoding.UTF8.GetBytes(valueB);

            return CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
        }

        /// <summary>
        /// Normalizes and validates an email address.
        /// </summary>
        private static string NormalizeEmailOrThrow(
            string? email,
            string errorMessage = "Email was not provided")
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException(errorMessage);
            }

            var normalizedEmail = email.Trim().ToLowerInvariant();

            if (!IsValidEmailFormat(normalizedEmail))
            {
                throw new InvalidOperationException("Invalid email format");
            }

            return normalizedEmail;
        }

        /// <summary>
        /// Checks whether an email address has a valid format.
        /// </summary>
        private static bool IsValidEmailFormat(string email)
        {
            return new EmailAddressAttribute().IsValid(email) &&
                   Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        /// <summary>
        /// Normalizes a required string value.
        /// </summary>
        private static string NormalizeOrThrow(string? value, string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(errorMessage);
            }

            return value.Trim().ToLowerInvariant();
        }
    }
}