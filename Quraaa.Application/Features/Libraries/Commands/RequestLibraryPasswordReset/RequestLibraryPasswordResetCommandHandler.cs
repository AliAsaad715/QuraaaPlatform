using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Library;

namespace Quraaa.Application.Features.Libraries.Commands.RequestLibraryPasswordReset
{
    public class RequestLibraryPasswordResetCommandHandler
        : BaseApplicationService<RequestLibraryPasswordResetCommandHandler>,
          IRequestHandler<RequestLibraryPasswordResetCommand, AppResult>
    {
        private const string GenericResultMessage =
            "If the email belongs to a library, a password reset code has been sent to it.";

        private readonly ILibraryRepository _libraryRepository;
        private readonly ILibraryPasswordResetRepository _resetRepository;
        private readonly ILibraryEmailOtpProtector _otpProtector;
        private readonly ILibraryEmailSender _emailSender;
        private readonly LibraryRegistrationOptions _options;

        public RequestLibraryPasswordResetCommandHandler(
            ILibraryRepository libraryRepository,
            ILibraryPasswordResetRepository resetRepository,
            ILibraryEmailOtpProtector otpProtector,
            ILibraryEmailSender emailSender,
            LibraryRegistrationOptions options,
            ILogger<RequestLibraryPasswordResetCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
            _resetRepository = resetRepository;
            _otpProtector = otpProtector;
            _emailSender = emailSender;
            _options = options;
        }

        public async Task<AppResult> Handle(
            RequestLibraryPasswordResetCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var normalizedEmail = request.Email.Trim().ToLowerInvariant();

                var library = await _libraryRepository.GetApprovedByEmailAsync(
                    normalizedEmail,
                    cancellationToken);

                if (library is null)
                {
                    // Unknown or unapproved library: say nothing that would
                    // distinguish it from a successful send.
                    Logger.LogInformation(
                        "A library password reset was requested for an address with no approved library.");
                    return;
                }

                var utcNow = DateTime.UtcNow;
                var challenge = await _resetRepository.GetByLibraryIdAsync(
                    library.Id,
                    cancellationToken);

                var isResend = challenge is not null
                    && challenge.CanVerifyAt(utcNow)
                    && challenge.CanSendAt(
                        utcNow,
                        _options.MaxEmailOtpSendsPerWindow,
                        _options.EmailOtpSendWindow);

                if (challenge is not null
                    && !isResend
                    && !challenge.CanStartCycleAt(
                        utcNow,
                        _options.MaxEmailOtpSendsPerWindow,
                        _options.EmailOtpSendWindow))
                {
                    // Cooling down or locked out. Still a generic success so the
                    // caller learns nothing about the address.
                    Logger.LogInformation(
                        "A library password reset for library {LibraryId} was throttled.",
                        library.Id);
                    return;
                }

                // A resend keeps the generation, so the code already in the
                // owner's inbox stays the valid one; a new cycle mints a new
                // generation and invalidates any earlier code.
                var generation = isResend ? challenge!.Generation : Guid.NewGuid();

                var otpCode = _otpProtector.DeriveCode(
                    library.Id,
                    library.UserId,
                    normalizedEmail,
                    generation);
                var codeHash = _otpProtector.HashCode(
                    otpCode,
                    library.Id,
                    library.UserId,
                    normalizedEmail,
                    generation);

                var expiresAtUtc = utcNow.Add(_options.EmailOtpLifetime);
                var resendAvailableAtUtc = utcNow.Add(_options.EmailOtpResendCooldown);

                if (challenge is null)
                {
                    challenge = new LibraryPasswordResetChallenge(
                        Guid.NewGuid(),
                        library.Id,
                        codeHash,
                        generation,
                        utcNow,
                        expiresAtUtc,
                        resendAvailableAtUtc);

                    await _resetRepository.AddAsync(challenge, cancellationToken);
                }
                else if (isResend)
                {
                    challenge.Resend(
                        codeHash,
                        generation,
                        utcNow,
                        expiresAtUtc,
                        resendAvailableAtUtc,
                        _options.MaxEmailOtpSendsPerWindow,
                        _options.EmailOtpSendWindow);
                }
                else
                {
                    challenge.StartNewCycle(
                        codeHash,
                        generation,
                        utcNow,
                        expiresAtUtc,
                        resendAvailableAtUtc,
                        _options.MaxEmailOtpSendsPerWindow,
                        _options.EmailOtpSendWindow);
                }

                await _resetRepository.SaveChangesAsync(cancellationToken);

                var deliveryAttemptStamp = challenge.ConcurrencyStamp;

                var deliveryStatus = await _emailSender.SendPasswordResetOtpAsync(
                    library.Email,
                    library.LibraryName,
                    otpCode,
                    _options.EmailOtpLifetime,
                    cancellationToken);

                if (deliveryStatus == EmailDeliveryStatus.NotSent
                    && challenge.TryCompensateDefiniteNotSent(generation, deliveryAttemptStamp))
                {
                    // The message definitely never left: refund one quota slot so
                    // an SMTP outage does not consume the owner's allowance.
                    // Best-effort — optimistic concurrency deliberately lets a
                    // later resend or reset win over this bookkeeping, and it
                    // must never turn an always-200 endpoint into an error.
                    try
                    {
                        await _resetRepository.SaveChangesAsync(cancellationToken);
                    }
                    catch (Exception exception) when (exception is not OperationCanceledException)
                    {
                        Logger.LogWarning(
                            exception,
                            "Could not refund the send quota after an undelivered password reset email for library {LibraryId}.",
                            library.Id);
                    }
                }
            }, GenericResultMessage);
        }
    }
}
