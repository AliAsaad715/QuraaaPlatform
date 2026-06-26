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

namespace Quraaa.Application.Features.Authentication.Commands.VerifyAdminLoginOtp
{
    public class VerifyAdminLoginOtpCommandHandler : BaseApplicationService<VerifyAdminLoginOtpCommandHandler>, IRequestHandler<VerifyAdminLoginOtpCommand, AppResult<AuthResponse>>
    {
        private readonly IIdentityService _identityService;
        private readonly IUserRepository _userRepository;
        private readonly IPhoneService _phoneService;
        private readonly IOtpCacheService _otpCacheService;

        private const string OtpKeyPrefix = "admin-login-otp";
        private const string InvalidAdminVerificationMessage = "Invalid or expired admin login verification code.";
        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan FailedAttemptWindow = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan VerificationLockout = TimeSpan.FromMinutes(5);

        public VerifyAdminLoginOtpCommandHandler(
            IIdentityService identityService,
            IUserRepository userRepository,
            IPhoneService phoneService,
            IOtpCacheService otpCacheService,
            ILogger<VerifyAdminLoginOtpCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _identityService = identityService;
            _userRepository = userRepository;
            _phoneService = phoneService;
            _otpCacheService = otpCacheService;
        }

        public async Task<AppResult<AuthResponse>> Handle(VerifyAdminLoginOtpCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync<VerifyAdminLoginOtpCommand, AuthResponse>(request, async () =>
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
                    throw new ApplicationBusinessException(InvalidAdminVerificationMessage);
                }

                if (!OtpCodesMatch(cachedOtp, request.OtpCode))
                {
                    await RecordFailedAttemptAsync(formattedPhone, clientTargetKey, cancellationToken);
                    throw new ApplicationBusinessException(InvalidAdminVerificationMessage);
                }

                var identity = await _identityService.GetUserIdentityByPhoneNumberAsync(formattedPhone);
                var adminProfile = await _userRepository.GetUserByPhoneNumberAsync(formattedPhone);
                var isAdminIdentity = identity is not null
                    && await _identityService.IsInRoleAsync(identity.UserId, Role.Admin.ToString());

                if (identity is null || adminProfile?.Role != Role.Admin || !isAdminIdentity)
                {
                    await _otpCacheService.ClearOtpAsync(formattedPhone, OtpKeyPrefix, cancellationToken);
                    await ClearVerificationStateAsync(formattedPhone, clientTargetKey, cancellationToken);
                    throw new ApplicationBusinessException(InvalidAdminVerificationMessage);
                }

                if (!identity.PhoneNumberConfirmed)
                {
                    var confirmResult = await _identityService.ConfirmPhoneNumberAsync(identity.UserId);
                    if (!confirmResult.Succeeded)
                    {
                        throw new ApplicationBusinessException("Admin login verification failed.");
                    }
                }

                await _otpCacheService.ClearOtpAsync(formattedPhone, OtpKeyPrefix, cancellationToken);
                await ClearVerificationStateAsync(formattedPhone, clientTargetKey, cancellationToken);

                return await _identityService.GenerateAuthTokensAsync(identity.UserId, formattedPhone);
            }, "Admin login verified successfully");
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
