using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Common;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Otp.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.User.Enums;

namespace Quraaa.Application.Features.Authentication.Commands.LibraryOwnerLogin
{
    public class LibraryOwnerLoginCommandHandler : BaseApplicationService<LibraryOwnerLoginCommandHandler>, IRequestHandler<LibraryOwnerLoginCommand, AppResult<AuthResponse>>
    {
        private readonly IIdentityService _identityService;
        private readonly IUserRepository _userRepository;
        private readonly ILibraryRepository _libraryRepository;
        private readonly ILibraryPasswordHasher _libraryPasswordHasher;
        private readonly IOtpCacheService _otpCacheService;

        private const string InvalidLibraryOwnerLoginMessage = "Invalid library email or password.";
        private const string CredentialAttemptKeyPrefix = "library-owner-login-credentials";
        private const int MaxFailedCredentialAttempts = 5;
        private static readonly TimeSpan CredentialFailedAttemptWindow = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan CredentialLockout = TimeSpan.FromMinutes(5);

        public LibraryOwnerLoginCommandHandler(
            IIdentityService identityService,
            IUserRepository userRepository,
            ILibraryRepository libraryRepository,
            ILibraryPasswordHasher libraryPasswordHasher,
            IOtpCacheService otpCacheService,
            ILogger<LibraryOwnerLoginCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _identityService = identityService;
            _userRepository = userRepository;
            _libraryRepository = libraryRepository;
            _libraryPasswordHasher = libraryPasswordHasher;
            _otpCacheService = otpCacheService;
        }

        public async Task<AppResult<AuthResponse>> Handle(LibraryOwnerLoginCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync<LibraryOwnerLoginCommand, AuthResponse>(request, async () =>
            {
                var normalizedEmail = NormalizeEmail(request.Email);
                var clientTargetKey = GetClientTargetKey(request.ClientIp);

                if (await IsCredentialLockedOutAsync(normalizedEmail, clientTargetKey, cancellationToken))
                {
                    throw new ApplicationBusinessException($"Too many invalid library owner login attempts. Please wait {CredentialLockout.TotalMinutes} minutes before trying again.");
                }

                var library = await _libraryRepository.GetApprovedByEmailAsync(normalizedEmail, cancellationToken);
                if (library is null)
                {
                    await RecordCredentialFailureAndThrowAsync(normalizedEmail, clientTargetKey, cancellationToken);
                    throw new InvalidOperationException("Library owner credential failure handling did not throw.");
                }

                var ownerProfile = await _userRepository.GetUserByIdAsync(library.UserId);
                if (ownerProfile is null)
                {
                    await RecordCredentialFailureAndThrowAsync(normalizedEmail, clientTargetKey, cancellationToken);
                    throw new InvalidOperationException("Library owner credential failure handling did not throw.");
                }

                var identity = await _identityService.GetUserIdentityByPhoneNumberAsync(ownerProfile.PhoneNumber);
                if (identity is null || identity.UserId != ownerProfile.Id)
                {
                    await RecordCredentialFailureAndThrowAsync(normalizedEmail, clientTargetKey, cancellationToken);
                    throw new InvalidOperationException("Library owner credential failure handling did not throw.");
                }

                // The library dashboard has its own password, set when the
                // library was registered — NOT the owner's account password.
                var isPasswordValid = _libraryPasswordHasher.Verify(
                    library.PasswordHash,
                    request.Password);

                if (!isPasswordValid)
                {
                    await RecordCredentialFailureAndThrowAsync(normalizedEmail, clientTargetKey, cancellationToken);
                }

                if (!identity.PhoneNumberConfirmed)
                {
                    await RecordCredentialFailureAndThrowAsync(normalizedEmail, clientTargetKey, cancellationToken);
                }

                var isLibraryOwnerIdentity = await _identityService
                    .IsLibraryOwnerIdentityAsync(identity.UserId);

                if (ownerProfile.Role != Role.LibraryOwner || !isLibraryOwnerIdentity)
                {
                    await RecordCredentialFailureAndThrowAsync(normalizedEmail, clientTargetKey, cancellationToken);
                }

                await ClearCredentialStateAsync(normalizedEmail, clientTargetKey, cancellationToken);

                var authResponse = await _identityService.GenerateLibraryOwnerAuthTokensAsync(
                    identity.UserId,
                    ownerProfile.PhoneNumber);
                if (authResponse is null)
                {
                    await RecordCredentialFailureAndThrowAsync(
                        normalizedEmail,
                        clientTargetKey,
                        cancellationToken);
                }

                return authResponse!;
            }, "Library owner logged in successfully");
        }

        private async Task RecordCredentialFailureAndThrowAsync(
            string normalizedEmail,
            string? clientTargetKey,
            CancellationToken cancellationToken)
        {
            var emailAttempts = await _otpCacheService.IncrementFailedVerificationAttemptAsync(
                normalizedEmail,
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

            if (emailAttempts < MaxFailedCredentialAttempts && clientAttempts < MaxFailedCredentialAttempts)
            {
                throw new ApplicationBusinessException(InvalidLibraryOwnerLoginMessage);
            }

            await _otpCacheService.RecordVerificationLockoutAsync(
                normalizedEmail,
                CredentialLockout,
                CredentialAttemptKeyPrefix,
                cancellationToken);
            await _otpCacheService.ClearFailedVerificationAttemptsAsync(
                normalizedEmail,
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

            throw new ApplicationBusinessException($"Too many invalid library owner login attempts. Please wait {CredentialLockout.TotalMinutes} minutes before trying again.");
        }

        private async Task<bool> IsCredentialLockedOutAsync(
            string normalizedEmail,
            string? clientTargetKey,
            CancellationToken cancellationToken)
        {
            if (await _otpCacheService.IsVerificationLockedOutAsync(normalizedEmail, CredentialAttemptKeyPrefix, cancellationToken))
            {
                return true;
            }

            return clientTargetKey is not null
                && await _otpCacheService.IsVerificationLockedOutAsync(clientTargetKey, CredentialAttemptKeyPrefix, cancellationToken);
        }

        private async Task ClearCredentialStateAsync(
            string normalizedEmail,
            string? clientTargetKey,
            CancellationToken cancellationToken)
        {
            await _otpCacheService.ClearFailedVerificationAttemptsAsync(normalizedEmail, CredentialAttemptKeyPrefix, cancellationToken);
            await _otpCacheService.ClearVerificationLockoutAsync(normalizedEmail, CredentialAttemptKeyPrefix, cancellationToken);

            if (clientTargetKey is not null)
            {
                await _otpCacheService.ClearFailedVerificationAttemptsAsync(clientTargetKey, CredentialAttemptKeyPrefix, cancellationToken);
                await _otpCacheService.ClearVerificationLockoutAsync(clientTargetKey, CredentialAttemptKeyPrefix, cancellationToken);
            }
        }

        private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

        private static string? GetClientTargetKey(string? clientIpAddress)
        {
            return string.IsNullOrWhiteSpace(clientIpAddress)
                ? null
                : $"ip:{clientIpAddress.Trim()}";
        }
    }
}
