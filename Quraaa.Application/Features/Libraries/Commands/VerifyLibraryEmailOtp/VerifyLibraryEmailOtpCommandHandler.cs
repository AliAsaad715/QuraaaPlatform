using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Libraries.Services;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Library;
using Quraaa.Domain.Library.Enums;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Libraries.Commands.VerifyLibraryEmailOtp
{
    public sealed class VerifyLibraryEmailOtpCommandHandler
        : BaseApplicationService<VerifyLibraryEmailOtpCommandHandler>,
          IRequestHandler<VerifyLibraryEmailOtpCommand, AppResult<LibraryEmailVerificationResponse>>
    {
        private const string InvalidOtpMessage = "The verification code is invalid or expired.";

        private readonly LibraryRegistrationSessionService _sessionService;
        private readonly ILibraryRepository _libraryRepository;
        private readonly ILibraryRegistrationRepository _registrationRepository;
        private readonly ILibraryEmailOtpProtector _otpProtector;
        private readonly ILibraryRegistrationTokenService _tokenService;
        private readonly LibraryRegistrationOptions _options;

        public VerifyLibraryEmailOtpCommandHandler(
            LibraryRegistrationSessionService sessionService,
            ILibraryRepository libraryRepository,
            ILibraryRegistrationRepository registrationRepository,
            ILibraryEmailOtpProtector otpProtector,
            ILibraryRegistrationTokenService tokenService,
            LibraryRegistrationOptions options,
            ILogger<VerifyLibraryEmailOtpCommandHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _sessionService = sessionService;
            _libraryRepository = libraryRepository;
            _registrationRepository = registrationRepository;
            _otpProtector = otpProtector;
            _tokenService = tokenService;
            _options = options;
        }

        public async Task<AppResult<LibraryEmailVerificationResponse>> Handle(
            VerifyLibraryEmailOtpCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<VerifyLibraryEmailOtpCommand, LibraryEmailVerificationResponse>(
                request,
                async () =>
                {
                    var session = await _sessionService.ResolveActiveAsync(
                        request.Token,
                        requireSubmitted: true,
                        cancellationToken,
                        allowCompleted: true);
                    var library = await _libraryRepository.GetByUserIdAsync(
                        session.UserId,
                        cancellationToken);

                    if (library is null)
                    {
                        throw new UnauthenticatedException();
                    }

                    var utcNow = DateTime.UtcNow;
                    var challenge = await _registrationRepository.GetChallengeByLibraryIdAsync(
                        library.Id,
                        cancellationToken);

                    if (challenge is null)
                    {
                        throw new ApplicationBusinessException(InvalidOtpMessage);
                    }

                    if (!challenge.IsCurrentGeneration(request.VerificationId))
                    {
                        throw new ApplicationBusinessException(InvalidOtpMessage);
                    }

                    if (library.EmailVerifiedAtUtc.HasValue)
                    {
                        // Replay (page reload / retry): report where the wizard
                        // actually stands rather than always pointing back at
                        // the Stripe step, and never mint a token for it.
                        return new LibraryEmailVerificationResponse(
                            library.Id,
                            challenge.Generation,
                            library.ApprovalStatus,
                            library.EmailVerifiedAtUtc.Value,
                            ResolveStage(session, library),
                            session.ExpiresAtUtc,
                            RegistrationToken: null);
                    }

                    if (challenge.IsLockedAt(utcNow))
                    {
                        throw new ConflictException(
                            "Too many incorrect verification attempts. Please wait before requesting another code.");
                    }

                    var codeMatches = challenge.CanVerifyAt(utcNow)
                        && challenge.CodeHash is not null
                        && _otpProtector.VerifyCode(
                            request.OtpCode,
                            library.Id,
                            library.UserId,
                            library.Email,
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
                            await _registrationRepository.SaveChangesAsync(cancellationToken);
                        }

                        throw new ApplicationBusinessException(InvalidOtpMessage);
                    }

                    library.VerifyEmail(utcNow);
                    challenge.MarkConsumed(utcNow);

                    // The session stays open for the optional Stripe wallet
                    // step, but on a NEW token and a much shorter window: from
                    // here the token can bind the payout account, so a copy of
                    // the original link (browser history, shared machine) must
                    // not still work.
                    var rotatedToken = _tokenService.Generate();

                    session.Reissue(
                        session.AuthenticationSessionId,
                        rotatedToken.TokenHash,
                        utcNow.Add(_options.WalletSetupSessionLifetime),
                        session.SubmittedAtUtc);

                    await _registrationRepository.SaveChangesAsync(cancellationToken);

                    return new LibraryEmailVerificationResponse(
                        library.Id,
                        challenge.Generation,
                        library.ApprovalStatus,
                        library.EmailVerifiedAtUtc!.Value,
                        ResolveStage(session, library),
                        session.ExpiresAtUtc,
                        rotatedToken.RawToken);
                },
                "Library email verified; the application is ready for admin review.");
        }

        /// <summary>
        /// Mirrors GetLibraryRegistrationContext so both endpoints agree on
        /// where the wizard stands.
        /// </summary>
        private static LibraryRegistrationStage ResolveStage(
            LibraryRegistrationSession session,
            LibraryAggregate library)
        {
            return session.CompletedAtUtc.HasValue
                || library.IsStripeWalletActive
                || library.ApprovalStatus == LibraryApprovalStatus.Rejected
                ? LibraryRegistrationStage.Completed
                : LibraryRegistrationStage.StripeWalletSetup;
        }
    }
}
