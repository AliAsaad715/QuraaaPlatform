using IdentityServer.Helpers;
using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
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
        private static readonly TimeSpan OtpExpiration = TimeSpan.FromMinutes(5);
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
                var isAdminIdentity = await _identityService.IsInRoleAsync(identity.UserId, Role.Admin.ToString());
                if (adminProfile?.Role != Role.Admin || !isAdminIdentity)
                {
                    await RecordCredentialFailureAndThrowAsync(formattedPhone, clientTargetKey, cancellationToken);
                }

                await ClearCredentialStateAsync(formattedPhone, clientTargetKey, cancellationToken);

                if (await IsVerificationLockedOutAsync(formattedPhone, clientTargetKey, cancellationToken))
                {
                    throw new ApplicationBusinessException($"Too many invalid OTP attempts. Please wait {VerificationLockout.TotalMinutes} minutes before requesting another OTP.");
                }

                if (await HasRecentRequestAsync(formattedPhone, clientTargetKey, cancellationToken))
                {
                    throw new ApplicationBusinessException($"Please wait at least {OtpLockout.TotalSeconds} seconds before requesting another OTP.");
                }

                await SendAdminLoginOtpAsync(formattedPhone, clientTargetKey, cancellationToken);
            }, "Admin login OTP sent successfully");
        }

        private async Task SendAdminLoginOtpAsync(
            string formattedPhone,
            string? clientTargetKey,
            CancellationToken cancellationToken)
        {
            var otpCode = RandomNumberGenerator.GetInt32(100000, 1_000_000).ToString();
            await _otpCacheService.SetOtpAsync(formattedPhone, otpCode, OtpExpiration, OtpKeyPrefix, cancellationToken);

            try
            {
                await RecordRequestLockoutAsync(formattedPhone, clientTargetKey, cancellationToken);
                await _firebaseSmsGateway.SendSmsRequestAsync(
                    formattedPhone,
                    otpCode,
                    cancellationToken: cancellationToken);
            }
            catch
            {
                await ClearRequestStateAsync(formattedPhone, clientTargetKey, cancellationToken);
                throw;
            }
        }

        private async Task<bool> HasRecentRequestAsync(
            string formattedPhone,
            string? clientTargetKey,
            CancellationToken cancellationToken)
        {
            if (await _otpCacheService.HasRecentOtpRequestAsync(formattedPhone, OtpLockout, OtpKeyPrefix, cancellationToken))
            {
                return true;
            }

            return clientTargetKey is not null
                && await _otpCacheService.HasRecentOtpRequestAsync(clientTargetKey, OtpLockout, OtpKeyPrefix, cancellationToken);
        }

        private async Task RecordRequestLockoutAsync(
            string formattedPhone,
            string? clientTargetKey,
            CancellationToken cancellationToken)
        {
            await _otpCacheService.RecordOtpRequestAsync(formattedPhone, OtpLockout, OtpKeyPrefix, cancellationToken);

            if (clientTargetKey is not null)
            {
                await _otpCacheService.RecordOtpRequestAsync(clientTargetKey, OtpLockout, OtpKeyPrefix, cancellationToken);
            }
        }

        private async Task ClearRequestStateAsync(
            string formattedPhone,
            string? clientTargetKey,
            CancellationToken cancellationToken)
        {
            await _otpCacheService.ClearOtpAsync(formattedPhone, OtpKeyPrefix, cancellationToken);
            await _otpCacheService.ClearOtpRequestAsync(formattedPhone, OtpKeyPrefix, cancellationToken);

            if (clientTargetKey is not null)
            {
                await _otpCacheService.ClearOtpRequestAsync(clientTargetKey, OtpKeyPrefix, cancellationToken);
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
    }
}
