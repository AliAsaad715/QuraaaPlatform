using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Libraries.Commands.ResetLibraryPassword
{
    public class ResetLibraryPasswordCommandHandler
        : BaseApplicationService<ResetLibraryPasswordCommandHandler>,
          IRequestHandler<ResetLibraryPasswordCommand, AppResult>
    {
        // One message for every failure mode below, so a caller cannot tell an
        // unknown address from a wrong or expired code.
        private const string InvalidResetMessage =
            "The verification code is invalid or expired.";

        private readonly ILibraryRepository _libraryRepository;
        private readonly ILibraryPasswordResetRepository _resetRepository;
        private readonly ILibraryEmailOtpProtector _otpProtector;
        private readonly ILibraryPasswordHasher _libraryPasswordHasher;
        private readonly IIdentityService _identityService;
        private readonly LibraryRegistrationOptions _options;

        public ResetLibraryPasswordCommandHandler(
            ILibraryRepository libraryRepository,
            ILibraryPasswordResetRepository resetRepository,
            ILibraryEmailOtpProtector otpProtector,
            ILibraryPasswordHasher libraryPasswordHasher,
            IIdentityService identityService,
            LibraryRegistrationOptions options,
            ILogger<ResetLibraryPasswordCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
            _resetRepository = resetRepository;
            _otpProtector = otpProtector;
            _libraryPasswordHasher = libraryPasswordHasher;
            _identityService = identityService;
            _options = options;
        }

        public async Task<AppResult> Handle(
            ResetLibraryPasswordCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var normalizedEmail = request.Email.Trim().ToLowerInvariant();

                // Tracked, because the new password hash has to persist.
                var library = await _libraryRepository.GetApprovedByEmailForUpdateAsync(
                    normalizedEmail,
                    cancellationToken);

                if (library is null)
                {
                    throw new ApplicationBusinessException(InvalidResetMessage);
                }

                var utcNow = DateTime.UtcNow;
                var challenge = await _resetRepository.GetByLibraryIdAsync(
                    library.Id,
                    cancellationToken);

                if (challenge is null)
                {
                    throw new ApplicationBusinessException(InvalidResetMessage);
                }

                // A locked-out challenge deliberately falls through to the
                // generic failure below: reporting the lockout separately would
                // tell an anonymous caller that this email belongs to a library.
                var codeMatches = challenge.CanVerifyAt(utcNow)
                    && challenge.CodeHash is not null
                    && _otpProtector.VerifyCode(
                        request.OtpCode,
                        library.Id,
                        library.UserId,
                        normalizedEmail,
                        challenge.Generation,
                        challenge.CodeHash);

                if (!codeMatches)
                {
                    if (challenge.CanVerifyAt(utcNow))
                    {
                        challenge.RecordFailedAttempt(
                            utcNow,
                            _options.MaxEmailOtpVerificationAttempts,
                            _options.EmailOtpVerificationLockout);
                        await _resetRepository.SaveChangesAsync(cancellationToken);
                    }

                    throw new ApplicationBusinessException(InvalidResetMessage);
                }

                // Checked only after the code proves the caller owns the inbox,
                // so this cannot be used to probe the owner's account password.
                if (await _identityService.CheckPasswordAsync(library.UserId, request.NewPassword))
                {
                    throw new ApplicationBusinessException(
                        LibraryPasswordRules.MustDifferFromAccountPasswordMessage,
                        nameof(ResetLibraryPasswordCommand.NewPassword));
                }

                library.SetPasswordHash(
                    _libraryPasswordHasher.Hash(request.NewPassword),
                    library.UserId);
                challenge.MarkConsumed(utcNow);

                // Both repositories share the request's DbContext, so this one
                // save commits the new password and the burnt code together.
                await _resetRepository.SaveChangesAsync(cancellationToken);

                // Whoever knew the old password loses their dashboard session:
                // a reset that left it live would not remediate anything.
                await _identityService.RevokeActiveSessionsAsync(library.UserId);

                Logger.LogInformation(
                    "Library {LibraryId} dashboard password was reset by email code.",
                    library.Id);
            }, "Library password reset successfully");
        }
    }
}
