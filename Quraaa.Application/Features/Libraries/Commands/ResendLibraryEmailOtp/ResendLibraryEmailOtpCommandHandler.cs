using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Libraries.Services;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Library;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Libraries.Commands.ResendLibraryEmailOtp
{
    public sealed class ResendLibraryEmailOtpCommandHandler
        : BaseApplicationService<ResendLibraryEmailOtpCommandHandler>,
          IRequestHandler<ResendLibraryEmailOtpCommand, AppResult<LibraryEmailOtpResponse>>
    {
        private readonly LibraryRegistrationSessionService _sessionService;
        private readonly ILibraryRepository _libraryRepository;
        private readonly ILibraryRegistrationRepository _registrationRepository;
        private readonly ILibraryEmailOtpProtector _otpProtector;
        private readonly ILibraryEmailSender _emailSender;
        private readonly LibraryRegistrationOptions _options;

        public ResendLibraryEmailOtpCommandHandler(
            LibraryRegistrationSessionService sessionService,
            ILibraryRepository libraryRepository,
            ILibraryRegistrationRepository registrationRepository,
            ILibraryEmailOtpProtector otpProtector,
            ILibraryEmailSender emailSender,
            LibraryRegistrationOptions options,
            ILogger<ResendLibraryEmailOtpCommandHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _sessionService = sessionService;
            _libraryRepository = libraryRepository;
            _registrationRepository = registrationRepository;
            _otpProtector = otpProtector;
            _emailSender = emailSender;
            _options = options;
        }

        public async Task<AppResult<LibraryEmailOtpResponse>> Handle(
            ResendLibraryEmailOtpCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<ResendLibraryEmailOtpCommand, LibraryEmailOtpResponse>(
                request,
                async () =>
                {
                    var session = await _sessionService.ResolveActiveAsync(
                        request.Token,
                        requireSubmitted: true,
                        cancellationToken);
                    var library = await _libraryRepository.GetByUserIdAsync(
                        session.UserId,
                        cancellationToken);

                    if (library is null || library.EmailVerifiedAtUtc.HasValue)
                    {
                        throw new UnauthenticatedException();
                    }

                    var utcNow = DateTime.UtcNow;
                    var challenge = await _registrationRepository.GetChallengeByLibraryIdAsync(
                        library.Id,
                        cancellationToken);

                    if (challenge is not null
                        && !challenge.CanSendAt(
                            utcNow,
                            _options.MaxEmailOtpSendsPerWindow,
                            _options.EmailOtpSendWindow))
                    {
                        throw new ConflictException(
                            "A verification code cannot be sent yet. Please wait before trying again.");
                    }

                    // A challenge keeps one stable verification id, generation, and
                    // derived code. Concurrent or retried sends therefore cannot put
                    // a stale, different code in the user's inbox.
                    var challengeId = challenge?.Id ?? Guid.NewGuid();
                    var generation = challenge?.Generation ?? Guid.NewGuid();
                    var otpCode = _otpProtector.DeriveCode(
                        library.Id,
                        library.UserId,
                        library.Email,
                        generation);
                    var codeHash = _otpProtector.HashCode(
                        otpCode,
                        library.Id,
                        library.UserId,
                        library.Email,
                        generation);
                    var expiresAtUtc = utcNow.Add(_options.EmailOtpLifetime);
                    var resendAvailableAtUtc = utcNow.Add(_options.EmailOtpResendCooldown);

                    if (challenge is null)
                    {
                        challenge = new LibraryEmailVerificationChallenge(
                            challengeId,
                            library.Id,
                            codeHash,
                            generation,
                            utcNow,
                            expiresAtUtc,
                            resendAvailableAtUtc);
                        await _registrationRepository.AddChallengeAsync(challenge, cancellationToken);
                    }
                    else
                    {
                        challenge.Issue(
                            codeHash,
                            generation,
                            utcNow,
                            expiresAtUtc,
                            resendAvailableAtUtc,
                            _options.MaxEmailOtpSendsPerWindow,
                            _options.EmailOtpSendWindow);
                    }

                    await _registrationRepository.SaveChangesAsync(cancellationToken);

                    var emailDeliveryStatus = await _emailSender.SendVerificationOtpAsync(
                        library.Email,
                        library.LibraryName,
                        challenge.Generation,
                        otpCode,
                        _options.EmailOtpLifetime,
                        cancellationToken);

                    if (emailDeliveryStatus == EmailDeliveryStatus.NotSent)
                    {
                        await BestEffortCompensateDefiniteNotSentAsync(
                            challenge,
                            generation,
                            challenge.ConcurrencyStamp);
                    }

                    return new LibraryEmailOtpResponse(
                        library.Id,
                        challenge.Generation,
                        LibraryEmailMasker.Mask(library.Email),
                        emailDeliveryStatus,
                        challenge.ExpiresAtUtc,
                        challenge.ResendAvailableAtUtc);
                },
                "Library email verification delivery attempt completed.");
        }

        private async Task BestEffortCompensateDefiniteNotSentAsync(
            LibraryEmailVerificationChallenge challenge,
            Guid attemptedGeneration,
            Guid deliveryAttemptStamp)
        {
            try
            {
                if (!challenge.TryCompensateDefiniteNotSent(
                        attemptedGeneration,
                        deliveryAttemptStamp))
                {
                    return;
                }

                await _registrationRepository.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception exception)
            {
                // Optimistic concurrency deliberately makes a later resend or verify
                // win over this best-effort compensation.
                Logger.LogWarning(
                    exception,
                    "Could not compensate definite SMTP non-delivery for challenge {ChallengeId} verification {VerificationId}.",
                    challenge.Id,
                    attemptedGeneration);
            }
        }
    }
}
