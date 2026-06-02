using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.Auth;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces;
using RoadmapPlatform.Application.Interfaces.Auth;
using RoadmapPlatform.Infrastructure.Configurations;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using System.Security.Cryptography;
using System.Text;

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
            string purpose)
        {
            provider = NormalizeOrThrow(provider, "Provider was not provided");
            email = NormalizeOrThrow(email, "Email was not provided");
            purpose = NormalizeOrThrow(purpose, "Purpose was not provided");

            var userExists = await _dbContext.Users
                .AnyAsync(x => x.UserId == userId);

            if (!userExists)
            {
                throw new NotFoundException("User was not found");
            }

            await EnsureResendCooldownHasPassedAsync(userId, provider, email, purpose);
            await InvalidateExistingTokensAsync(userId, provider, email, purpose);

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

            await _dbContext.SaveChangesAsync();

            await _emailSender.SendEmailAsync(
                email,
                "Your verification code",
                $"Your verification code is: {otp}\n\nThis code expires in {_settings.ExpirationMinutes} minutes.");
        }

        public async Task VerifyVerificationCodeAsync(
            Guid userId,
            string provider,
            string email,
            string purpose,
            string otp)
        {
            provider = NormalizeOrThrow(provider, "Provider was not provided");
            email = NormalizeOrThrow(email, "Email was not provided");
            purpose = NormalizeOrThrow(purpose, "Purpose was not provided");
            otp = NormalizeOrThrow(otp, "OTP was not provided");

            var token = await GetLatestUsableTokenAsync(userId, provider, email, purpose);

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
                await _dbContext.SaveChangesAsync();

                throw new InvalidOperationException("Invalid verification code");
            }

            token.UsedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
        }

        public async Task<EmailVerificationResultDto> VerifyRegistrationEmailAsync(string email, string otp)
        {
            email = NormalizeOrThrow(email, "Email was not provided");

            var localProvider = await _dbContext.UserAuthProviders
                .FirstOrDefaultAsync(x =>
                    x.Provider == AuthProviders.Local &&
                    x.ProviderUserId == email);

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
                otp);

            localProvider.EmailVerifiedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return new EmailVerificationResultDto
            {
                UserId = localProvider.UserId,
                Email = email
            };
        }

        public async Task ResendRegistrationVerificationAsync(string email)
        {
            email = NormalizeOrThrow(email, "Email was not provided");

            var localProvider = await _dbContext.UserAuthProviders
                .FirstOrDefaultAsync(x =>
                    x.Provider == AuthProviders.Local &&
                    x.ProviderUserId == email);

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
                EmailVerificationPurposes.Register);
        }

        public async Task ResendLinkedLocalVerificationAsync(Guid userId)
        {
            var localProvider = await _dbContext.UserAuthProviders
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.Provider == AuthProviders.Local);

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
                EmailVerificationPurposes.LinkLocal);
        }

        public async Task VerifyLinkedLocalEmailAsync(Guid userId, string otp)
        {
            otp = NormalizeOrThrow(otp, "OTP was not provided");

            var localProvider = await _dbContext.UserAuthProviders
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.Provider == AuthProviders.Local);

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
                otp);

            localProvider.EmailVerifiedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
        }

        public async Task RequestLocalEmailChangeAsync(Guid userId, string newEmail)
        {
            newEmail = NormalizeOrThrow(newEmail, "New email was not provided");

            var localProvider = await _dbContext.UserAuthProviders
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.Provider == AuthProviders.Local);

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
                    x.UserId != userId);

            if (emailExists)
            {
                throw new ConflictException("Email is already registered");
            }

            localProvider.PendingEmail = newEmail;

            await _dbContext.SaveChangesAsync();

            await SendVerificationCodeAsync(
                userId,
                AuthProviders.Local,
                newEmail,
                EmailVerificationPurposes.ChangeEmail);
        }

        public async Task VerifyLocalEmailChangeAsync(Guid userId, string otp)
        {
            otp = NormalizeOrThrow(otp, "OTP was not provided");

            var localProvider = await _dbContext.UserAuthProviders
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.Provider == AuthProviders.Local);

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
                    x.UserId != userId);

            if (emailExists)
            {
                throw new ConflictException("Email is already registered");
            }

            await VerifyVerificationCodeAsync(
                userId,
                AuthProviders.Local,
                pendingEmail,
                EmailVerificationPurposes.ChangeEmail,
                otp);

            localProvider.Email = pendingEmail;
            localProvider.ProviderUserId = pendingEmail;
            localProvider.PendingEmail = null;
            localProvider.EmailVerifiedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
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
            string purpose)
        {
            var token = await _dbContext.EmailVerificationTokens
                .Where(x =>
                    x.UserId == userId &&
                    x.Provider == provider &&
                    x.Email == email &&
                    x.Purpose == purpose &&
                    x.UsedAt == null)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

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
            string purpose)
        {
            var latestToken = await _dbContext.EmailVerificationTokens
                .Where(x =>
                    x.UserId == userId &&
                    x.Provider == provider &&
                    x.Email == email &&
                    x.Purpose == purpose)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (latestToken == null)
            {
                return;
            }

            var nextAllowedAt = latestToken.CreatedAt.AddSeconds(_settings.ResendCooldownSeconds);

            if (DateTime.UtcNow < nextAllowedAt)
            {
                var remainingSeconds = (int)Math.Ceiling((nextAllowedAt - DateTime.UtcNow).TotalSeconds);

                throw new InvalidOperationException(
                    $"Please wait {remainingSeconds} seconds before requesting another code");
            }
        }

        private async Task InvalidateExistingTokensAsync(
            Guid userId,
            string provider,
            string email,
            string purpose)
        {
            var existingTokens = await _dbContext.EmailVerificationTokens
                .Where(x =>
                    x.UserId == userId &&
                    x.Provider == provider &&
                    x.Email == email &&
                    x.Purpose == purpose &&
                    x.UsedAt == null)
                .ToListAsync();

            foreach (var token in existingTokens)
            {
                token.UsedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();
        }

        private static bool FixedTimeEquals(string valueA, string valueB)
        {
            var bytesA = Encoding.UTF8.GetBytes(valueA);
            var bytesB = Encoding.UTF8.GetBytes(valueB);

            return CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
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
