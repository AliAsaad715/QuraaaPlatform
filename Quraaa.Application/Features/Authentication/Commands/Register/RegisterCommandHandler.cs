using IdentityServer.Helpers;
using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Otp.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.User;
using Quraaa.Domain.User.Enums;
using System.Security.Cryptography;

namespace Quraaa.Application.Features.Authentication.Commands.Register
{
    public class RegisterCommandHandler : BaseApplicationService<RegisterCommandHandler>, IRequestHandler<RegisterCommand, AppResult>
    {
        private readonly IIdentityService _identityService;
        private readonly IUserRepository _userRepository;
        private readonly IPhoneService _phoneService;
        private readonly IOtpCacheService _otpCacheService;
        private readonly IFirebaseSmsGateway _firebaseSmsGateway;
        private readonly IAuthenticationUnitOfWork _authenticationUnitOfWork;

        private const string OtpKeyPrefix = "register-otp";
        private static readonly TimeSpan OtpExpiration = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan OtpLockout = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan VerificationLockout = TimeSpan.FromMinutes(5);

        public RegisterCommandHandler(
            IIdentityService identityService,
            IUserRepository userRepository,
            IPhoneService phoneService,
            IOtpCacheService otpCacheService,
            IFirebaseSmsGateway firebaseSmsGateway,
            IAuthenticationUnitOfWork authenticationUnitOfWork,
            ILogger<RegisterCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _identityService = identityService;
            _userRepository = userRepository;
            _phoneService = phoneService;
            _otpCacheService = otpCacheService;
            _firebaseSmsGateway = firebaseSmsGateway;
            _authenticationUnitOfWork = authenticationUnitOfWork;
        }

        public async Task<AppResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
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
                    throw new ApplicationBusinessException($"Too many invalid OTP attempts. Please wait {VerificationLockout.TotalMinutes} minutes before requesting another OTP.");
                }

                var existingIdentity = await _identityService.GetUserIdentityByPhoneNumberAsync(formattedPhone);
                if (existingIdentity?.PhoneNumberConfirmed == true)
                {
                    throw new ApplicationBusinessException("Phone number is already registered.");
                }

                if (existingIdentity is not null)
                {
                    var existingRequestLease = await AcquireRequestLeaseAsync(
                        formattedPhone,
                        clientTargetKey,
                        cancellationToken);

                    await SendRegistrationOtpAsync(
                        formattedPhone,
                        existingRequestLease,
                        cancellationToken);
                    throw new ApplicationBusinessException("Phone number is pending verification. We sent a new OTP code.");
                }

                var userProfile = await _userRepository.GetUserByPhoneNumberAsync(formattedPhone);
                if (userProfile is not null)
                {
                    throw new ApplicationBusinessException("A user profile already exists for this phone number.");
                }

                var requestLease = await AcquireRequestLeaseAsync(
                    formattedPhone,
                    clientTargetKey,
                    cancellationToken);

                try
                {
                    await _authenticationUnitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
                    {
                        var id = Guid.NewGuid();
                        var roleName = Role.User.ToString();
                        var identityResult = await _identityService.CreateUserIdentityAsync(
                            id,
                            formattedPhone,
                            request.Password,
                            roleName,
                            phoneNumberConfirmed: false);

                        if (!identityResult.Succeeded)
                        {
                            var allErrors = string.Join(" | ", identityResult.Errors);
                            throw new ApplicationBusinessException(allErrors);
                        }

                        if (string.IsNullOrWhiteSpace(identityResult.PasswordHash))
                        {
                            throw new ApplicationBusinessException("Pending user password hash was not returned.");
                        }

                        userProfile = new UserAggregate(
                            id,
                            request.FirstName,
                            request.LastName,
                            formattedPhone,
                            identityResult.PasswordHash,
                            request.Gender,
                            Role.User,
                            request.DateOfBirth);

                        AddInterests(userProfile, request.Interests);
                        await _userRepository.AddUserAsync(userProfile, transactionCancellationToken);
                        await _userRepository.SaveChangesAsync(transactionCancellationToken);
                    }, cancellationToken);
                }
                catch
                {
                    await CleanupFailedOtpRequestAsync(
                        formattedPhone,
                        requestLease,
                        otpCode: null,
                        CancellationToken.None);
                    throw;
                }

                await SendRegistrationOtpAsync(
                    formattedPhone,
                    requestLease,
                    cancellationToken);
            }, "Registration OTP sent successfully");
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

        private static void AddInterests(UserAggregate userProfile, IEnumerable<Guid> interests)
        {
            foreach (var interest in interests)
            {
                userProfile.AddInterest(interest);
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
                        "Failed to clear an owned registration OTP during registration cleanup.");
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
                        "Failed to release a registration OTP client lease during registration cleanup.");
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
                    "Failed to release a registration OTP phone lease during registration cleanup.");
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
