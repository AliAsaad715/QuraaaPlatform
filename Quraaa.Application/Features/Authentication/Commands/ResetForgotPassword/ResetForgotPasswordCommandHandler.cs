using IdentityServer.Helpers;
using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Otp.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;
using System.Security.Cryptography;
using System.Text;

namespace Quraaa.Application.Features.Authentication.Commands.ResetForgotPassword
{
    public class ResetForgotPasswordCommandHandler : BaseApplicationService<ResetForgotPasswordCommandHandler>, IRequestHandler<ResetForgotPasswordCommand, AppResult>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPhoneService _phoneService;
        private readonly IOtpCacheService _otpCacheService;
        private readonly IIdentityService _identityService;

        private const string OtpKeyPrefix = "forgot-password-otp";
        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan FailedAttemptWindow = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan VerificationLockout = TimeSpan.FromMinutes(5);

        public ResetForgotPasswordCommandHandler(
            IUserRepository userRepository,
            IPhoneService phoneService,
            IOtpCacheService otpCacheService,
            IIdentityService identityService,
            ILogger<ResetForgotPasswordCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _userRepository = userRepository;
            _phoneService = phoneService;
            _otpCacheService = otpCacheService;
            _identityService = identityService;
        }

        public async Task<AppResult> Handle(ResetForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var formattedPhone = _phoneService.FormatToE164(request.PhoneNumber);
                if (string.IsNullOrEmpty(formattedPhone))
                {
                    throw new ApplicationBusinessException("Invalid phone number format.");
                }

                var clientTargetKey = GetClientTargetKey(request.ClientIp);

                if (await IsVerificationLockedOutAsync(formattedPhone, clientTargetKey, cancellationToken))
                {
                    throw new ApplicationBusinessException($"Too many invalid OTP attempts. Please wait {VerificationLockout.TotalMinutes} minutes before trying again.");
                }

                var cachedOtp = await _otpCacheService.GetOtpAsync(formattedPhone, OtpKeyPrefix, cancellationToken);

                if (string.IsNullOrEmpty(cachedOtp))
                {
                    throw new ApplicationBusinessException("OTP code is expired or was not requested.");
                }

                if (!OtpCodesMatch(cachedOtp, request.OtpCode))
                {
                    await RecordFailedAttemptAsync(formattedPhone, clientTargetKey, cancellationToken);
                    throw new ApplicationBusinessException("Invalid OTP code.");
                }

                await _otpCacheService.ClearOtpAsync(formattedPhone, OtpKeyPrefix, cancellationToken);
                await ClearVerificationStateAsync(formattedPhone, clientTargetKey, cancellationToken);

                var user = await _userRepository.GetUserByPhoneNumberAsync(formattedPhone);
                if (user == null)
                {
                    throw new NotFoundException("User was not found.");
                }

                var (succeeded, updatedPasswordHash, errors) = await _identityService.ResetPasswordAsync(user.Id, request.NewPassword);
                if (!succeeded)
                {
                    var firstError = errors.FirstOrDefault() ?? "Password reset failed.";
                    throw new ApplicationBusinessException(firstError);
                }

                if (string.IsNullOrWhiteSpace(updatedPasswordHash))
                {
                    throw new ApplicationBusinessException("Password was changed, but the updated password hash was not returned.");
                }

                user.UpdatePasswordHash(updatedPasswordHash, user.Id);
                await _userRepository.SaveChangesAsync();

            }, "Password reset successfully");
        }

        private async Task RecordFailedAttemptAsync(
            string formattedPhone,
            string? clientTargetKey,
            CancellationToken cancellationToken)
        {
            var phoneAttempts = await _otpCacheService.IncrementFailedVerificationAttemptAsync(
                formattedPhone,
                FailedAttemptWindow,
                OtpKeyPrefix,
                cancellationToken);

            var clientAttempts = 0;
            if (clientTargetKey is not null)
            {
                clientAttempts = await _otpCacheService.IncrementFailedVerificationAttemptAsync(
                    clientTargetKey,
                    FailedAttemptWindow,
                    OtpKeyPrefix,
                    cancellationToken);
            }

            if (phoneAttempts < MaxFailedAttempts && clientAttempts < MaxFailedAttempts)
            {
                return;
            }

            await _otpCacheService.ClearOtpAsync(formattedPhone, OtpKeyPrefix, cancellationToken);
            await _otpCacheService.RecordVerificationLockoutAsync(formattedPhone, VerificationLockout, OtpKeyPrefix, cancellationToken);
            await _otpCacheService.ClearFailedVerificationAttemptsAsync(formattedPhone, OtpKeyPrefix, cancellationToken);

            if (clientTargetKey is not null)
            {
                await _otpCacheService.RecordVerificationLockoutAsync(clientTargetKey, VerificationLockout, OtpKeyPrefix, cancellationToken);
                await _otpCacheService.ClearFailedVerificationAttemptsAsync(clientTargetKey, OtpKeyPrefix, cancellationToken);
            }

            throw new ApplicationBusinessException($"Too many invalid OTP attempts. Please wait {VerificationLockout.TotalMinutes} minutes before trying again.");
        }

        private async Task<bool> IsVerificationLockedOutAsync(
            string formattedPhone,
            string? clientTargetKey,
            CancellationToken cancellationToken)
        {
            if (await _otpCacheService.IsVerificationLockedOutAsync(formattedPhone, OtpKeyPrefix, cancellationToken))
            {
                return true;
            }

            return clientTargetKey is not null
                && await _otpCacheService.IsVerificationLockedOutAsync(clientTargetKey, OtpKeyPrefix, cancellationToken);
        }

        private async Task ClearVerificationStateAsync(
            string formattedPhone,
            string? clientTargetKey,
            CancellationToken cancellationToken)
        {
            await _otpCacheService.ClearFailedVerificationAttemptsAsync(formattedPhone, OtpKeyPrefix, cancellationToken);
            await _otpCacheService.ClearVerificationLockoutAsync(formattedPhone, OtpKeyPrefix, cancellationToken);

            if (clientTargetKey is not null)
            {
                await _otpCacheService.ClearFailedVerificationAttemptsAsync(clientTargetKey, OtpKeyPrefix, cancellationToken);
                await _otpCacheService.ClearVerificationLockoutAsync(clientTargetKey, OtpKeyPrefix, cancellationToken);
            }
        }

        private static bool OtpCodesMatch(string cachedOtp, string submittedOtp)
        {
            var cachedBytes = Encoding.UTF8.GetBytes(cachedOtp);
            var submittedBytes = Encoding.UTF8.GetBytes(submittedOtp);

            return CryptographicOperations.FixedTimeEquals(cachedBytes, submittedBytes);
        }

        private static string? GetClientTargetKey(string? clientIpAddress)
        {
            return string.IsNullOrWhiteSpace(clientIpAddress)
                ? null
                : $"ip:{clientIpAddress.Trim()}";
        }
    }
}
