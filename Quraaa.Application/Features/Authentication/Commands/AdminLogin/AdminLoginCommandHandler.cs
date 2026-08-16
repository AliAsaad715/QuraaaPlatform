using IdentityServer.Helpers;
using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Otp.Exceptions;
using Quraaa.Application.Features.Otp.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.User.Enums;
using System.Security.Cryptography;

namespace Quraaa.Application.Features.Authentication.Commands.AdminLogin
{
    public class AdminLoginCommandHandler : BaseApplicationService<AdminLoginCommandHandler>, IRequestHandler<AdminLoginCommand, AppResult>
    {
        private readonly IIdentityService _identityService;
        private readonly IUserRepository _userRepository;
        private readonly IPhoneService _phoneService;
        private readonly IOtpCacheService _otpCacheService;
        private readonly IFirebaseSmsGateway _firebaseSmsGateway;

        private const string OtpKeyPrefix = "admin-login-otp";
        private const string CredentialAttemptKeyPrefix = "admin-login-credentials";
        private const string InvalidAdminLoginMessage = "Invalid admin phone number or password.";
        private const int MaxFailedCredentialAttempts = 5;
        private static readonly TimeSpan OtpExpiration = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan OtpLockout = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan VerificationLockout = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan CredentialFailedAttemptWindow = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan CredentialLockout = TimeSpan.FromMinutes(5);

        public AdminLoginCommandHandler(
            IIdentityService identityService,
            IUserRepository userRepository,
            IPhoneService phoneService,
            IOtpCacheService otpCacheService,
            IFirebaseSmsGateway firebaseSmsGateway,
            ILogger<AdminLoginCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _identityService = identityService;
            _userRepository = userRepository;
            _phoneService = phoneService;
            _otpCacheService = otpCacheService;
            _firebaseSmsGateway = firebaseSmsGateway;
        }

        public async Task<AppResult> Handle(AdminLoginCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var formattedPhone = _phoneService.FormatToE164(request.PhoneNumber);
                if (string.IsNullOrEmpty(formattedPhone))
                {
                    throw new ApplicationBusinessException("Invalid phone number format.");
                }

                var clientTargetKey = GetClientTargetKey(request.ClientIp);

                if (await IsCredentialLockedOutAsync(formattedPhone, clientTargetKey, cancellationToken))
                {
                    throw new ApplicationBusinessException($"Too many invalid admin login attempts. Please wait {CredentialLockout.TotalMinutes} minutes before trying again.");
                }

                var identity = await _identityService.GetUserIdentityByPhoneNumberAsync(formattedPhone);
                if (identity is null)
                {
                    await RecordCredentialFailureAndThrowAsync(formattedPhone, clientTargetKey, cancellationToken);
                    throw new ApplicationBusinessException(InvalidAdminLoginMessage);
                }

                var isPasswordValid = await _identityService.CheckPasswordAsync(identity.UserId, request.Password);
                if (!isPasswordValid)
                {
                    await RecordCredentialFailureAndThrowAsync(formattedPhone, clientTargetKey, cancellationToken);
                }

                var adminProfile = await _userRepository.GetUserByPhoneNumberAsync(formattedPhone);
                var isSuperAdminIdentity = await _identityService.IsInRoleAsync(
                    identity.UserId,
                    Role.SuperAdmin.ToString());
                if (adminProfile?.Role != Role.SuperAdmin || !isSuperAdminIdentity)
                {
                    await RecordCredentialFailureAndThrowAsync(formattedPhone, clientTargetKey, cancellationToken);
                }

                await ClearCredentialStateAsync(formattedPhone, clientTargetKey, cancellationToken);

                if (await IsVerificationLockedOutAsync(formattedPhone, clientTargetKey, cancellationToken))
                {
                    throw new ApplicationBusinessException($"Too many invalid OTP attempts. Please wait {VerificationLockout.TotalMinutes} minutes before requesting another OTP.");
                }

                var requestLease = await AcquireRequestLeaseAsync(
                    formattedPhone,
                    clientTargetKey,
                    cancellationToken);

                await SendAdminLoginOtpAsync(
                    formattedPhone,
                    requestLease,
                    cancellationToken);
            }, "Admin login OTP sent successfully");
        }

        private async Task SendAdminLoginOtpAsync(
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
                    purpose: "admin-login",
                    cancellationToken: cancellationToken);
            }
            catch (SmsDispatchException exception)
                when (ownsOtp && exception.Outcome == SmsDispatchOutcome.DefinitelyNotDispatched)
            {
                Logger.LogWarning(
                    exception,
                    "Clearing a stored admin-login OTP because the SMS request was definitely not dispatched.");
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
                    "Retaining a stored admin-login OTP because the SMS gateway dispatch outcome is unknown.");
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
                        "Failed to clear an owned admin-login OTP after a definite dispatch failure.");
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
                        "Failed to release an admin-login OTP client lease during cleanup.");
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
                    "Failed to release an admin-login OTP phone lease during cleanup.");
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

        private async Task RecordCredentialFailureAndThrowAsync(
            string formattedPhone,
            string? clientTargetKey,
            CancellationToken cancellationToken)
        {
            var phoneAttempts = await _otpCacheService.IncrementFailedVerificationAttemptAsync(
                formattedPhone,
                CredentialFailedAttemptWindow,
                CredentialAttemptKeyPrefix,
                cancellationToken);

            var clientAttempts = 0;
            if (clientTargetKey is not null)
            {
                clientAttempts = await _otpCacheService.IncrementFailedVerificationAttemptAsync(
                    clientTargetKey,
                    CredentialFailedAttemptWindow,
                    CredentialAttemptKeyPrefix,
                    cancellationToken);
            }

            if (phoneAttempts < MaxFailedCredentialAttempts && clientAttempts < MaxFailedCredentialAttempts)
            {
                throw new ApplicationBusinessException(InvalidAdminLoginMessage);
            }

            await _otpCacheService.RecordVerificationLockoutAsync(
                formattedPhone,
                CredentialLockout,
                CredentialAttemptKeyPrefix,
                cancellationToken);
            await _otpCacheService.ClearFailedVerificationAttemptsAsync(
                formattedPhone,
                CredentialAttemptKeyPrefix,
                cancellationToken);

            if (clientTargetKey is not null)
            {
                await _otpCacheService.RecordVerificationLockoutAsync(
                    clientTargetKey,
                    CredentialLockout,
                    CredentialAttemptKeyPrefix,
                    cancellationToken);
                await _otpCacheService.ClearFailedVerificationAttemptsAsync(
                    clientTargetKey,
                    CredentialAttemptKeyPrefix,
                    cancellationToken);
            }

            throw new ApplicationBusinessException($"Too many invalid admin login attempts. Please wait {CredentialLockout.TotalMinutes} minutes before trying again.");
        }

        private async Task<bool> IsCredentialLockedOutAsync(
            string formattedPhone,
            string? clientTargetKey,
            CancellationToken cancellationToken)
        {
            if (await _otpCacheService.IsVerificationLockedOutAsync(formattedPhone, CredentialAttemptKeyPrefix, cancellationToken))
            {
                return true;
            }

            return clientTargetKey is not null
                && await _otpCacheService.IsVerificationLockedOutAsync(clientTargetKey, CredentialAttemptKeyPrefix, cancellationToken);
        }

        private async Task ClearCredentialStateAsync(
            string formattedPhone,
            string? clientTargetKey,
            CancellationToken cancellationToken)
        {
            await _otpCacheService.ClearFailedVerificationAttemptsAsync(formattedPhone, CredentialAttemptKeyPrefix, cancellationToken);
            await _otpCacheService.ClearVerificationLockoutAsync(formattedPhone, CredentialAttemptKeyPrefix, cancellationToken);

            if (clientTargetKey is not null)
            {
                await _otpCacheService.ClearFailedVerificationAttemptsAsync(clientTargetKey, CredentialAttemptKeyPrefix, cancellationToken);
                await _otpCacheService.ClearVerificationLockoutAsync(clientTargetKey, CredentialAttemptKeyPrefix, cancellationToken);
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
