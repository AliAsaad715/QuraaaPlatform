using IdentityServer.Helpers;
using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Common;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Otp.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.User.Enums;
using System.Security.Cryptography;
using System.Text;

namespace Quraaa.Application.Features.Authentication.Commands.VerifyRegisterOtp
{
    public class VerifyRegisterOtpCommandHandler : BaseApplicationService<VerifyRegisterOtpCommandHandler>, IRequestHandler<VerifyRegisterOtpCommand, AppResult<AuthResponse>>
    {
        private readonly IIdentityService _identityService;
        private readonly IUserRepository _userRepository;
        private readonly IPhoneService _phoneService;
        private readonly IOtpCacheService _otpCacheService;

        private const string OtpKeyPrefix = "register-otp";
        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan FailedAttemptWindow = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan VerificationLockout = TimeSpan.FromMinutes(5);

        public VerifyRegisterOtpCommandHandler(
            IIdentityService identityService,
            IUserRepository userRepository,
            IPhoneService phoneService,
            IOtpCacheService otpCacheService,
            ILogger<VerifyRegisterOtpCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _identityService = identityService;
            _userRepository = userRepository;
            _phoneService = phoneService;
            _otpCacheService = otpCacheService;
        }

        public async Task<AppResult<AuthResponse>> Handle(VerifyRegisterOtpCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync<VerifyRegisterOtpCommand, AuthResponse>(request, async () =>
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

                var identity = await _identityService.GetUserIdentityByPhoneNumberAsync(formattedPhone);
                if (identity is null)
                {
                    throw new ApplicationBusinessException("Registration was not started for this phone number.");
                }

                if (identity.PhoneNumberConfirmed)
                {
                    throw new ApplicationBusinessException("Phone number is already verified.");
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

                var userProfile = await _userRepository.GetUserByIdAsync(identity.UserId);
                var profileMatchesRegularRegistration = userProfile is not null
                    && string.Equals(
                        userProfile.PhoneNumber,
                        formattedPhone,
                        StringComparison.Ordinal)
                    && userProfile.Role == Role.User;
                var isRegularUserIdentity = await _identityService
                    .IsRegularUserIdentityAsync(identity.UserId);

                if (!profileMatchesRegularRegistration || !isRegularUserIdentity)
                {
                    var recoveryCandidate = userProfile is null
                        || (profileMatchesRegularRegistration && !isRegularUserIdentity);
                    var incompleteRegistrationDeleted = recoveryCandidate
                        && await _identityService
                            .TryDeleteIncompleteUnconfirmedRegularRegistrationAsync(
                                identity.UserId,
                                cancellationToken);

                    if (!incompleteRegistrationDeleted)
                    {
                        throw new ApplicationBusinessException("Pending registration is invalid.");
                    }

                    await _otpCacheService.ClearOtpAsync(
                        formattedPhone,
                        OtpKeyPrefix,
                        cancellationToken);
                    await ClearVerificationStateAsync(
                        formattedPhone,
                        clientTargetKey,
                        cancellationToken);

                    throw new ApplicationBusinessException(
                        "Incomplete registration was cleared. Please wait up to 60 seconds, then start registration again.");
                }

                var confirmResult = await _identityService.ConfirmPhoneNumberAsync(identity.UserId);
                if (!confirmResult.Succeeded)
                {
                    var allErrors = string.Join(" | ", confirmResult.Errors);
                    throw new ApplicationBusinessException(allErrors);
                }

                await _otpCacheService.ClearOtpAsync(formattedPhone, OtpKeyPrefix, cancellationToken);
                await ClearVerificationStateAsync(formattedPhone, clientTargetKey, cancellationToken);

                var authResponse = await _identityService
                    .GenerateRegularUserAuthTokensAsync(identity.UserId, formattedPhone);
                return authResponse
                    ?? throw new ApplicationBusinessException("Pending registration is invalid.");
            }, "Registration verified successfully");
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
