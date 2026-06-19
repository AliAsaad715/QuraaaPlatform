using IdentityServer.Helpers;
using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Otp.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Authentication.Commands.ForgotPassword
{
    public class ForgotPasswordCommandHandler : BaseApplicationService<ForgotPasswordCommandHandler>, IRequestHandler<ForgotPasswordCommand, AppResult>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPhoneService _phoneService;
        private readonly IOtpCacheService _otpCacheService;
        private readonly IFirebaseSmsGateway _firebaseSmsGateway;

        private static readonly TimeSpan OtpExpiration = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan OtpLockout = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan VerificationLockout = TimeSpan.FromMinutes(5);

        public ForgotPasswordCommandHandler(
            IUserRepository userRepository,
            IPhoneService phoneService,
            IOtpCacheService otpCacheService,
            IFirebaseSmsGateway firebaseSmsGateway,
            ILogger<ForgotPasswordCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _userRepository = userRepository;
            _phoneService = phoneService;
            _otpCacheService = otpCacheService;
            _firebaseSmsGateway = firebaseSmsGateway;
        }

        public async Task<AppResult> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
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

                if (await HasRecentRequestAsync(formattedPhone, clientTargetKey, cancellationToken))
                {
                    throw new ApplicationBusinessException($"Please wait at least {OtpLockout.TotalSeconds} seconds before requesting another OTP.");
                }

                var user = await _userRepository.GetUserByPhoneNumberAsync(formattedPhone);
                if (user == null)
                {
                    await RecordRequestLockoutAsync(formattedPhone, clientTargetKey, cancellationToken);
                    return;
                }

                var otpCode = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1_000_000).ToString();

                await _otpCacheService.SetOtpAsync(formattedPhone, otpCode, OtpExpiration, cancellationToken);

                try
                {
                    await RecordRequestLockoutAsync(formattedPhone, clientTargetKey, cancellationToken);
                    await _firebaseSmsGateway.SendSmsRequestAsync(
                        formattedPhone,
                        otpCode,
                        request.SmsGatewayDeviceToken,
                        cancellationToken);
                }
                catch
                {
                    await ClearRequestStateAsync(formattedPhone, clientTargetKey, cancellationToken);
                    throw;
                }

            }, "Forgot password request processed");
        }

        private async Task<bool> HasRecentRequestAsync(
            string formattedPhone,
            string? clientTargetKey,
            CancellationToken cancellationToken)
        {
            if (await _otpCacheService.HasRecentOtpRequestAsync(formattedPhone, OtpLockout, cancellationToken))
            {
                return true;
            }

            return clientTargetKey is not null
                && await _otpCacheService.HasRecentOtpRequestAsync(clientTargetKey, OtpLockout, cancellationToken);
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

        private async Task RecordRequestLockoutAsync(
            string formattedPhone,
            string? clientTargetKey,
            CancellationToken cancellationToken)
        {
            await _otpCacheService.RecordOtpRequestAsync(formattedPhone, OtpLockout, cancellationToken);

            if (clientTargetKey is not null)
            {
                await _otpCacheService.RecordOtpRequestAsync(clientTargetKey, OtpLockout, cancellationToken);
            }
        }

        private async Task ClearRequestStateAsync(
            string formattedPhone,
            string? clientTargetKey,
            CancellationToken cancellationToken)
        {
            await _otpCacheService.ClearOtpAsync(formattedPhone, cancellationToken);
            await _otpCacheService.ClearOtpRequestAsync(formattedPhone, cancellationToken);

            if (clientTargetKey is not null)
            {
                await _otpCacheService.ClearOtpRequestAsync(clientTargetKey, cancellationToken);
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
