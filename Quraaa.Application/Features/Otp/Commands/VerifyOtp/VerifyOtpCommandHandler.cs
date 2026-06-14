using IdentityServer.Helpers;
using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Otp.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using System.Security.Cryptography;
using System.Text;

namespace Quraaa.Application.Features.Otp.Commands.VerifyOtp
{
    public class VerifyOtpCommandHandler : BaseApplicationService<VerifyOtpCommandHandler>, IRequestHandler<VerifyOtpCommand, AppResult>
    {
        private readonly IOtpCacheService _otpCacheService;
        private readonly IPhoneService _phoneService;

        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan FailedAttemptWindow = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan VerificationLockout = TimeSpan.FromMinutes(5);

        public VerifyOtpCommandHandler(
            IOtpCacheService otpCacheService,
            IPhoneService phoneService,
            ILogger<VerifyOtpCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _otpCacheService = otpCacheService;
            _phoneService = phoneService;
        }

        public async Task<AppResult> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var formattedPhone = _phoneService.FormatToE164(request.PhoneNumber);
                if (string.IsNullOrEmpty(formattedPhone))
                {
                    throw new ApplicationBusinessException("Invalid phone number format.");
                }

                var clientTargetKey = GetClientTargetKey(request.ClientIpAddress);

                if (await IsVerificationLockedOutAsync(formattedPhone, clientTargetKey, cancellationToken))
                {
                    throw new ApplicationBusinessException($"Too many invalid OTP attempts. Please wait {VerificationLockout.TotalMinutes} minutes before trying again.");
                }

                var cachedOtp = await _otpCacheService.GetOtpAsync(formattedPhone, cancellationToken);

                if (string.IsNullOrEmpty(cachedOtp))
                {
                    throw new ApplicationBusinessException("OTP code is expired or was not requested.");
                }

                if (!OtpCodesMatch(cachedOtp, request.Code))
                {
                    await RecordFailedAttemptAsync(formattedPhone, clientTargetKey, cancellationToken);
                    throw new ApplicationBusinessException("Invalid OTP code.");
                }

                await _otpCacheService.ClearOtpAsync(formattedPhone, cancellationToken);
                await ClearVerificationStateAsync(formattedPhone, clientTargetKey, cancellationToken);

            }, "OTP verified successfully");
        }

        private async Task RecordFailedAttemptAsync(
            string formattedPhone,
            string? clientTargetKey,
            CancellationToken cancellationToken)
        {
            var phoneAttempts = await _otpCacheService.IncrementFailedVerificationAttemptAsync(
                formattedPhone,
                FailedAttemptWindow,
                cancellationToken);

            var clientAttempts = 0;
            if (clientTargetKey is not null)
            {
                clientAttempts = await _otpCacheService.IncrementFailedVerificationAttemptAsync(
                    clientTargetKey,
                    FailedAttemptWindow,
                    cancellationToken);
            }

            if (phoneAttempts < MaxFailedAttempts && clientAttempts < MaxFailedAttempts)
            {
                return;
            }

            await _otpCacheService.ClearOtpAsync(formattedPhone, cancellationToken);
            await _otpCacheService.RecordVerificationLockoutAsync(formattedPhone, VerificationLockout, cancellationToken);
            await _otpCacheService.ClearFailedVerificationAttemptsAsync(formattedPhone, cancellationToken);

            if (clientTargetKey is not null)
            {
                await _otpCacheService.RecordVerificationLockoutAsync(clientTargetKey, VerificationLockout, cancellationToken);
                await _otpCacheService.ClearFailedVerificationAttemptsAsync(clientTargetKey, cancellationToken);
            }

            throw new ApplicationBusinessException($"Too many invalid OTP attempts. Please wait {VerificationLockout.TotalMinutes} minutes before trying again.");
        }

        private async Task<bool> IsVerificationLockedOutAsync(
            string formattedPhone,
            string? clientTargetKey,
            CancellationToken cancellationToken)
        {
            if (await _otpCacheService.IsVerificationLockedOutAsync(formattedPhone, cancellationToken))
            {
                return true;
            }

            return clientTargetKey is not null
                && await _otpCacheService.IsVerificationLockedOutAsync(clientTargetKey, cancellationToken);
        }

        private async Task ClearVerificationStateAsync(
            string formattedPhone,
            string? clientTargetKey,
            CancellationToken cancellationToken)
        {
            await _otpCacheService.ClearFailedVerificationAttemptsAsync(formattedPhone, cancellationToken);
            await _otpCacheService.ClearVerificationLockoutAsync(formattedPhone, cancellationToken);

            if (clientTargetKey is not null)
            {
                await _otpCacheService.ClearFailedVerificationAttemptsAsync(clientTargetKey, cancellationToken);
                await _otpCacheService.ClearVerificationLockoutAsync(clientTargetKey, cancellationToken);
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
