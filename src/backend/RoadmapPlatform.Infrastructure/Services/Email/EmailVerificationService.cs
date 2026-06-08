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
    public class EmailVerificationService : IEmailVerificationService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IEmailSender _emailSender;
        private readonly EmailVerificationSettings _settings;

        public EmailVerificationService(
            ApplicationDbContext dbContext,
            IEmailSender emailSender,
            IOptions<EmailVerificationSettings> options)
        {
            _dbContext = dbContext;
            _emailSender = emailSender;
            _settings = options.Value;
        }

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

        public async Task<EmailVerificationResultDto> VerifyRegistrationEmailAsync(
            string email,
            string otp,
            CancellationToken cancellationToken = default)
        {
            email = NormalizeEmailOrThrow(email);

            var localProvider = await _dbContext.UserAuthProviders
                .FirstOrDefaultAsync(x =>
                    x.Provider == AuthProviders.Local &&
                    x.ProviderUserId == email,
                    cancellationToken);

            if (localProvider == null)
            {
                throw new NotFoundException("Local login method was not found");
            }

            if (localProvider.EmailVerifiedAt != null)
            {
                throw new InvalidOperationException("Email is already verified");
            }

            await VerifyVerificationCodeAsync(
                localProvider.UserId,
                AuthProviders.Local,
                email,
                EmailVerificationPurposes.Register,
                otp,
                cancellationToken);

            localProvider.EmailVerifiedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new EmailVerificationResultDto
            {
                UserId = localProvider.UserId,
                Email = email
            };
        }

        public async Task ResendRegistrationVerificationAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            email = NormalizeEmailOrThrow(email);

            var localProvider = await _dbContext.UserAuthProviders
                .FirstOrDefaultAsync(x =>
                    x.Provider == AuthProviders.Local &&
                    x.ProviderUserId == email,
                    cancellationToken);

            if (localProvider == null)
            {
                throw new NotFoundException("Local login method was not found");
            }

            if (localProvider.EmailVerifiedAt != null)
            {
                throw new InvalidOperationException("Email is already verified");
            }

            await SendVerificationCodeAsync(
                localProvider.UserId,
                AuthProviders.Local,
                email,
                EmailVerificationPurposes.Register,
                cancellationToken);
        }

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

        private static bool FixedTimeEquals(string valueA, string valueB)
        {
            var bytesA = Encoding.UTF8.GetBytes(valueA);
            var bytesB = Encoding.UTF8.GetBytes(valueB);

            return CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
        }


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

        private static bool IsValidEmailFormat(string email)
        {
            return new EmailAddressAttribute().IsValid(email) &&
                   Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

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