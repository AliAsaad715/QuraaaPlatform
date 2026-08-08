using IdentityServer.Helpers;
using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Otp.Exceptions;
using Quraaa.Application.Features.Otp.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using System.Security.Cryptography;

namespace Quraaa.Application.Features.Otp.Commands.SendOtp
{
    public class SendOtpCommandHandler : BaseApplicationService<SendOtpCommandHandler>, IRequestHandler<SendOtpCommand, AppResult>
    {
        private readonly IOtpCacheService _otpCacheService;
        private readonly IFirebaseSmsGateway _firebaseSmsGateway;
        private readonly IPhoneService _phoneService;

        private const string OtpKeyPrefix = "standalone-otp";
        private static readonly TimeSpan OtpExpiration = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan OtpLockout = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan VerificationLockout = TimeSpan.FromMinutes(5);

        public SendOtpCommandHandler(
            IOtpCacheService otpCacheService,
            IFirebaseSmsGateway firebaseSmsGateway,
            IPhoneService phoneService,
            ILogger<SendOtpCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _otpCacheService = otpCacheService;
            _firebaseSmsGateway = firebaseSmsGateway;
            _phoneService = phoneService;
        }

        public async Task<AppResult> Handle(SendOtpCommand request, CancellationToken cancellationToken)
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
                    throw new ApplicationBusinessException($"Too many invalid OTP attempts. Please wait {VerificationLockout.TotalMinutes} minutes before requesting another OTP.");
                }

                var requestLease = await AcquireRequestLeaseAsync(
                    formattedPhone,
                    clientTargetKey,
                    cancellationToken);

                await SendOtpAsync(formattedPhone, requestLease, cancellationToken);

            }, "OTP sent successfully");
        }

        private async Task SendOtpAsync(
            string formattedPhone,
            OtpRequestLease requestLease,
            CancellationToken cancellationToken)
        {
            var otpCode = RandomNumberGenerator.GetInt32(100000, 1_000_000).ToString();
            var ownsOtp = false;

            try
            {
                ownsOtp = await _otpCacheService.TrySetOwnedOtpAsync(
                    formattedPhone,
                    otpCode,
                    requestLease.OwnerToken,
                    OtpExpiration,
                    OtpKeyPrefix,
                    cancellationToken);

                if (!ownsOtp)
                {
                    throw new ApplicationBusinessException(
                        $"Please wait at least {OtpLockout.TotalSeconds} seconds before requesting another OTP.");
                }

                await _firebaseSmsGateway.SendSmsRequestAsync(
                    formattedPhone,
                    otpCode,
                    purpose: "standalone",
                    cancellationToken: cancellationToken);
            }
            catch (SmsDispatchException exception)
                when (ownsOtp && exception.Outcome == SmsDispatchOutcome.DefinitelyNotDispatched)
            {
                Logger.LogWarning(
                    exception,
                    "Clearing a stored standalone OTP because the SMS request was definitely not dispatched.");
                await CleanupFailedOtpRequestAsync(
                    formattedPhone,
                    requestLease,
                    CancellationToken.None,
                    ownedOtpCode: otpCode);
                throw;
            }
            catch (Exception exception) when (ownsOtp)
            {
                Logger.LogWarning(
                    exception,
                    "Retaining a stored standalone OTP because the SMS gateway dispatch outcome is unknown.");
                throw;
            }
            catch
            {
                await CleanupFailedOtpRequestAsync(
                    formattedPhone,
                    requestLease,
                    CancellationToken.None);
                throw;
            }
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

        private async Task<OtpRequestLease> AcquireRequestLeaseAsync(
            string formattedPhone,
            string? clientTargetKey,
            CancellationToken cancellationToken)
        {
            var ownerToken = Guid.NewGuid().ToString("N");
            var phoneAcquired = await _otpCacheService.TryAcquireOtpRequestAsync(
                formattedPhone,
                ownerToken,
                OtpLockout,
                OtpKeyPrefix,
                cancellationToken);

            if (!phoneAcquired)
            {
                throw new ApplicationBusinessException(
                    $"Please wait at least {OtpLockout.TotalSeconds} seconds before requesting another OTP.");
            }

            try
            {
                if (clientTargetKey is not null)
                {
                    var clientAcquired = await _otpCacheService.TryAcquireOtpRequestAsync(
                        clientTargetKey,
                        ownerToken,
                        OtpLockout,
                        OtpKeyPrefix,
                        cancellationToken);

                    if (!clientAcquired)
                    {
                        throw new ApplicationBusinessException(
                            $"Please wait at least {OtpLockout.TotalSeconds} seconds before requesting another OTP.");
                    }
                }

                return new OtpRequestLease(ownerToken, clientTargetKey);
            }
            catch
            {
                await CleanupFailedOtpRequestAsync(
                    formattedPhone,
                    new OtpRequestLease(ownerToken, clientTargetKey),
                    CancellationToken.None);
                throw;
            }
        }

        private async Task CleanupFailedOtpRequestAsync(
            string formattedPhone,
            OtpRequestLease requestLease,
            CancellationToken cancellationToken,
            string? ownedOtpCode = null)
        {
            if (ownedOtpCode is not null)
            {
                try
                {
                    await _otpCacheService.ClearOwnedOtpAsync(
                        formattedPhone,
                        ownedOtpCode,
                        requestLease.OwnerToken,
                        OtpKeyPrefix,
                        cancellationToken);
                }
                catch (Exception cleanupException)
                {
                    Logger.LogWarning(
                        cleanupException,
                        "Failed to clear an owned standalone OTP after a definite dispatch failure.");
                }
            }

            if (requestLease.ClientTargetKey is not null)
            {
                try
                {
                    await _otpCacheService.ReleaseOtpRequestAsync(
                        requestLease.ClientTargetKey,
                        requestLease.OwnerToken,
                        OtpKeyPrefix,
                        cancellationToken);
                }
                catch (Exception cleanupException)
                {
                    Logger.LogWarning(
                        cleanupException,
                        "Failed to release a standalone OTP client lease during cleanup.");
                }
            }

            try
            {
                await _otpCacheService.ReleaseOtpRequestAsync(
                    formattedPhone,
                    requestLease.OwnerToken,
                    OtpKeyPrefix,
                    cancellationToken);
            }
            catch (Exception cleanupException)
            {
                Logger.LogWarning(
                    cleanupException,
                    "Failed to release a standalone OTP phone lease during cleanup.");
            }
        }

        private static string? GetClientTargetKey(string? clientIpAddress)
        {
            return string.IsNullOrWhiteSpace(clientIpAddress)
                ? null
                : $"ip:{clientIpAddress.Trim()}";
        }

        private sealed record OtpRequestLease(
            string OwnerToken,
            string? ClientTargetKey);
    }
}
