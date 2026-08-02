using MediatR;
using Microsoft.Extensions.Logging;
using IdentityServer.Helpers;
using Quraaa.Application.Features.Authentication.Common;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Otp.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.User.Enums;
using System.Security.Cryptography;

namespace Quraaa.Application.Features.Authentication.Commands.Login
{
    public class LoginCommandHandler : BaseApplicationService<LoginCommandHandler>, IRequestHandler<LoginCommand, AppResult<AuthResponse>>
    {
        private readonly IUserRepository _repo;
        private readonly IIdentityService _identityService;
        private readonly IPhoneService _phoneService;
        private readonly IOtpCacheService _otpCacheService;
        private readonly IFirebaseSmsGateway _firebaseSmsGateway;

        private const string OtpKeyPrefix = "register-otp";
        private const string CredentialAttemptKeyPrefix = "user-login-credentials";
        private const int MaxFailedAccountAttempts = 5;
        private const int MaxFailedClientAttempts = 20;
        private static readonly TimeSpan OtpExpiration = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan OtpLockout = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan VerificationLockout = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan CredentialAttemptWindow = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan CredentialLockout = TimeSpan.FromMinutes(5);

        public LoginCommandHandler(
            ILogger<LoginCommandHandler> logger,
            IServiceProvider serviceProvider,
            IUserRepository repo,
            IIdentityService identityService,
            IPhoneService phoneService,
            IOtpCacheService otpCacheService,
            IFirebaseSmsGateway firebaseSmsGateway) : base(logger, serviceProvider)
        {
            _repo = repo;
            _identityService = identityService;
            _phoneService = phoneService;
            _otpCacheService = otpCacheService;
            _firebaseSmsGateway = firebaseSmsGateway;
        }

        public async Task<AppResult<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var formattedPhone = _phoneService.FormatToE164(request.PhoneNumber);
                if (string.IsNullOrEmpty(formattedPhone))
                {
                    throw new ApplicationBusinessException("Invalid phone number format.");
                }

                var clientTargetKey = GetClientTargetKey(request.ClientIp);
                if (await IsCredentialLockedOutAsync(
                        formattedPhone,
                        clientTargetKey,
                        cancellationToken))
                {
                    throw new ApplicationBusinessException(
                        $"Too many invalid login attempts. Please wait {CredentialLockout.TotalMinutes} minutes before trying again.");
                }

                var identity = await _identityService.GetUserIdentityByPhoneNumberAsync(formattedPhone);
                if (identity is null)
                {
                    await RecordCredentialFailureAndThrowAsync(
                        formattedPhone,
                        clientTargetKey,
                        cancellationToken);
                }

                var passwordIsValid = await _identityService.CheckPasswordAsync(
                    identity!.UserId,
                    request.Password);

                if (!passwordIsValid)
                {
                    await RecordCredentialFailureAndThrowAsync(
                        formattedPhone,
                        clientTargetKey,
                        cancellationToken);
                }

                var userProfile = await _repo.GetUserByPhoneNumberAsync(formattedPhone);
                if (userProfile is null)
                {
                    await RecordCredentialFailureAndThrowAsync(
                        formattedPhone,
                        clientTargetKey,
                        cancellationToken);
                }

                var profileRole = userProfile!.Role;
                var identityMatchesProfile = profileRole switch
                {
                    Role.User => await _identityService
                        .IsRegularUserIdentityAsync(identity.UserId),
                    Role.LibraryOwner => await _identityService
                        .IsLibraryOwnerIdentityAsync(identity.UserId),
                    _ => false
                };

                if (!identityMatchesProfile)
                {
                    await RecordCredentialFailureAndThrowAsync(
                        formattedPhone,
                        clientTargetKey,
                        cancellationToken);
                }

                if (!identity.PhoneNumberConfirmed)
                {
                    // Registration OTP verification only completes ordinary User
                    // registrations. Never send an owner/admin account into that flow.
                    if (profileRole != Role.User)
                    {
                        await RecordCredentialFailureAndThrowAsync(
                            formattedPhone,
                            clientTargetKey,
                            cancellationToken);
                    }

                    await ClearAccountCredentialStateAsync(formattedPhone, cancellationToken);

                    if (await IsVerificationLockedOutAsync(formattedPhone, clientTargetKey, cancellationToken))
                    {
                        throw new ApplicationBusinessException($"Too many invalid OTP attempts. Please wait {VerificationLockout.TotalMinutes} minutes before requesting another OTP.");
                    }

                    var requestLease = await AcquireRequestLeaseAsync(
                        formattedPhone,
                        clientTargetKey,
                        cancellationToken);

                    await SendRegistrationOtpAsync(
                        formattedPhone,
                        requestLease,
                        cancellationToken);
                    throw new ApplicationBusinessException("Phone number is pending verification. We sent a new OTP code.");
                }

                await ClearAccountCredentialStateAsync(formattedPhone, cancellationToken);

                var authResponse = profileRole switch
                {
                    Role.User => await _identityService.GenerateRegularUserAuthTokensAsync(
                        identity.UserId,
                        formattedPhone),
                    Role.LibraryOwner => await _identityService.GenerateLibraryOwnerAuthTokensAsync(
                        identity.UserId,
                        formattedPhone),
                    _ => null
                };

                if (authResponse is null)
                {
                    await RecordCredentialFailureAndThrowAsync(
                        formattedPhone,
                        clientTargetKey,
                        cancellationToken);
                }

                return authResponse!;
            }, "User logged in successfully");
        }

        private async Task SendRegistrationOtpAsync(
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
                    cancellationToken: cancellationToken);
            }
            catch
            {
                await CleanupFailedOtpRequestAsync(
                    formattedPhone,
                    requestLease,
                    ownsOtp ? otpCode : null,
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
                    otpCode: null,
                    CancellationToken.None);
                throw;
            }
        }

        private async Task CleanupFailedOtpRequestAsync(
            string formattedPhone,
            OtpRequestLease requestLease,
            string? otpCode,
            CancellationToken cancellationToken)
        {
            if (otpCode is not null)
            {
                try
                {
                    await _otpCacheService.ClearOwnedOtpAsync(
                        formattedPhone,
                        otpCode,
                        requestLease.OwnerToken,
                        OtpKeyPrefix,
                        cancellationToken);
                }
                catch (Exception cleanupException)
                {
                    Logger.LogWarning(
                        cleanupException,
                        "Failed to clear an owned registration OTP during login cleanup.");
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
                        "Failed to release a registration OTP client lease during login cleanup.");
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
                    "Failed to release a registration OTP phone lease during login cleanup.");
            }
        }

        private async Task RecordCredentialFailureAndThrowAsync(
            string formattedPhone,
            string? clientTargetKey,
            CancellationToken cancellationToken)
        {
            var accountAttempts = await _otpCacheService.IncrementFailedVerificationAttemptAsync(
                formattedPhone,
                CredentialAttemptWindow,
                CredentialAttemptKeyPrefix,
                cancellationToken);

            var clientAttempts = 0;
            if (clientTargetKey is not null)
            {
                clientAttempts = await _otpCacheService.IncrementFailedVerificationAttemptAsync(
                    clientTargetKey,
                    CredentialAttemptWindow,
                    CredentialAttemptKeyPrefix,
                    cancellationToken);
            }

            if (accountAttempts >= MaxFailedAccountAttempts)
            {
                await _otpCacheService.RecordVerificationLockoutAsync(
                    formattedPhone,
                    CredentialLockout,
                    CredentialAttemptKeyPrefix,
                    cancellationToken);
                await _otpCacheService.ClearFailedVerificationAttemptsAsync(
                    formattedPhone,
                    CredentialAttemptKeyPrefix,
                    cancellationToken);
            }

            if (clientTargetKey is not null
                && clientAttempts >= MaxFailedClientAttempts)
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

            throw new ApplicationBusinessException("Invalid phone number or password.");
        }

        private async Task<bool> IsCredentialLockedOutAsync(
            string formattedPhone,
            string? clientTargetKey,
            CancellationToken cancellationToken)
        {
            if (await _otpCacheService.IsVerificationLockedOutAsync(
                    formattedPhone,
                    CredentialAttemptKeyPrefix,
                    cancellationToken))
            {
                return true;
            }

            return clientTargetKey is not null
                && await _otpCacheService.IsVerificationLockedOutAsync(
                    clientTargetKey,
                    CredentialAttemptKeyPrefix,
                    cancellationToken);
        }

        private async Task ClearAccountCredentialStateAsync(
            string formattedPhone,
            CancellationToken cancellationToken)
        {
            await _otpCacheService.ClearFailedVerificationAttemptsAsync(
                formattedPhone,
                CredentialAttemptKeyPrefix,
                cancellationToken);
            await _otpCacheService.ClearVerificationLockoutAsync(
                formattedPhone,
                CredentialAttemptKeyPrefix,
                cancellationToken);
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
