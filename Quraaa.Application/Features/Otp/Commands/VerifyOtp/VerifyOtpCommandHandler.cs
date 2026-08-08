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

        private const string OtpKeyPrefix = "standalone-otp";
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

                var cachedOtp = await _otpCacheService.GetOtpAsync(formattedPhone, OtpKeyPrefix, cancellationToken);

                if (string.IsNullOrEmpty(cachedOtp))
                {
                    throw new ApplicationBusinessException("OTP code is expired or was not requested.");
                }

                if (!OtpCodesMatch(cachedOtp, request.Code))
                {
                    await RecordFailedAttemptAsync(
                        formattedPhone,
                        clientTargetKey,
                        cachedOtp,
                        CancellationToken.None);
                    throw new ApplicationBusinessException("Invalid OTP code.");
                }

                var consumed = await _otpCacheService.TryConsumeOtpAsync(
                    formattedPhone,
                    cachedOtp,
                    OtpKeyPrefix,
                    cancellationToken);

                if (!consumed)
                {
                    throw new ApplicationBusinessException("OTP code is expired or was replaced by a newer code.");
                }

                await ClearVerificationStateAsync(formattedPhone, clientTargetKey, CancellationToken.None);

            }, "OTP verified successfully");
        }

        private async Task RecordFailedAttemptAsync(
            string formattedPhone,
            string? clientTargetKey,
            string inspectedOtp,
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

            // Invalidate the inspected OTP if it is still current. A concurrent
            // resend must not prevent the threshold lockout below.
            await _otpCacheService.TryConsumeOtpAsync(
                formattedPhone,
                inspectedOtp,
                OtpKeyPrefix,
                cancellationToken);

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
            try
            {
                await _otpCacheService.ClearFailedVerificationAttemptsAsync(formattedPhone, OtpKeyPrefix, cancellationToken);
                await _otpCacheService.ClearVerificationLockoutAsync(formattedPhone, OtpKeyPrefix, cancellationToken);

                if (clientTargetKey is not null)
                {
                    await _otpCacheService.ClearFailedVerificationAttemptsAsync(clientTargetKey, OtpKeyPrefix, cancellationToken);
                    await _otpCacheService.ClearVerificationLockoutAsync(clientTargetKey, OtpKeyPrefix, cancellationToken);
                }
            }
            catch (Exception exception)
            {
                Logger.LogWarning(
                    exception,
                    "Failed to clear standalone OTP verification state after consuming the code.");
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
