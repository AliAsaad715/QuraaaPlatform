using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Libraries.Services;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Library;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Libraries.Commands.RegisterLibrary
{
    public sealed class RegisterLibraryCommandHandler
        : BaseApplicationService<RegisterLibraryCommandHandler>,
          IRequestHandler<RegisterLibraryCommand, AppResult<LibraryRegistrationSubmissionResponse>>
    {
        private readonly ILibraryRepository _libraryRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILibraryPasswordHasher _libraryPasswordHasher;
        private readonly IIdentityService _identityService;
        private readonly ILibraryImageStorageService _libraryImageStorageService;
        private readonly ILibraryRegistrationRepository _registrationRepository;
        private readonly LibraryRegistrationSessionService _sessionService;
        private readonly ILibraryEmailOtpProtector _otpProtector;
        private readonly ILibraryEmailSender _emailSender;
        private readonly LibraryRegistrationOptions _options;

        public RegisterLibraryCommandHandler(
            ILibraryRepository libraryRepository,
            IUserRepository userRepository,
            ILibraryPasswordHasher libraryPasswordHasher,
            IIdentityService identityService,
            ILibraryImageStorageService libraryImageStorageService,
            ILibraryRegistrationRepository registrationRepository,
            LibraryRegistrationSessionService sessionService,
            ILibraryEmailOtpProtector otpProtector,
            ILibraryEmailSender emailSender,
            LibraryRegistrationOptions options,
            ILogger<RegisterLibraryCommandHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
            _userRepository = userRepository;
            _libraryPasswordHasher = libraryPasswordHasher;
            _identityService = identityService;
            _libraryImageStorageService = libraryImageStorageService;
            _registrationRepository = registrationRepository;
            _sessionService = sessionService;
            _otpProtector = otpProtector;
            _emailSender = emailSender;
            _options = options;
        }

        public async Task<AppResult<LibraryRegistrationSubmissionResponse>> Handle(
            RegisterLibraryCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<RegisterLibraryCommand, LibraryRegistrationSubmissionResponse>(
                request,
                async () =>
                {
                    var session = await _sessionService.ResolveActiveAsync(
                        request.Token,
                        requireSubmitted: false,
                        cancellationToken);

                    if (session.SubmittedAtUtc.HasValue)
                    {
                        throw new ConflictException("Library registration details have already been submitted.");
                    }

                    var user = await _userRepository.GetUserByIdAsync(session.UserId);
                    if (user is null)
                    {
                        throw new NotFoundException("User was not found.");
                    }

                    if (await _libraryRepository.ExistsByUserIdAsync(session.UserId))
                    {
                        throw new DomainException(LibraryErrorCodes.DuplicateLibraryForUser);
                    }

                    var normalizedEmail = request.Email.Trim().ToLowerInvariant();
                    if (await _libraryRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
                    {
                        throw new DomainException(LibraryErrorCodes.DuplicateLibraryEmail);
                    }

                    // The library dashboard must not be reachable with the
                    // owner's personal account password: two separate
                    // credentials, two separate secrets.
                    if (await _identityService.CheckPasswordAsync(session.UserId, request.Password))
                    {
                        throw new ApplicationBusinessException(
                            LibraryPasswordRules.MustDifferFromAccountPasswordMessage,
                            nameof(RegisterLibraryCommand.Password));
                    }

                    string? libraryImagePath = null;
                    string? headerImagePath = null;
                    LibraryAggregate? library = null;
                    LibraryEmailVerificationChallenge? challenge = null;
                    string? otpCode = null;
                    var databaseCommitted = false;

                    try
                    {
                        libraryImagePath = await _libraryImageStorageService.SaveLibraryImageAsync(
                            request.LibraryImage!,
                            cancellationToken);
                        headerImagePath = await _libraryImageStorageService.SaveHeaderImageAsync(
                            request.HeaderImage!,
                            cancellationToken);

                        // Uploads can take long enough for the user to log out or
                        // reissue the link. Recheck the credential immediately before
                        // creating durable state so revocation wins that race.
                        session = await _sessionService.ResolveActiveAsync(
                            request.Token,
                            requireSubmitted: false,
                            cancellationToken);
                        if (session.SubmittedAtUtc.HasValue)
                        {
                            throw new ConflictException(
                                "Library registration details have already been submitted.");
                        }

                        var utcNow = DateTime.UtcNow;
                        var libraryId = Guid.NewGuid();
                        var generation = Guid.NewGuid();
                        otpCode = _otpProtector.DeriveCode(
                            libraryId,
                            session.UserId,
                            normalizedEmail,
                            generation);
                        var codeHash = _otpProtector.HashCode(
                            otpCode,
                            libraryId,
                            session.UserId,
                            normalizedEmail,
                            generation);

                        library = new LibraryAggregate(
                            libraryId,
                            request.LibraryName.Trim(),
                            request.Location.Trim(),
                            libraryImagePath,
                            headerImagePath,
                            normalizedEmail,
                            session.UserId,
                            _libraryPasswordHasher.Hash(request.Password));

                        challenge = new LibraryEmailVerificationChallenge(
                            Guid.NewGuid(),
                            libraryId,
                            codeHash,
                            generation,
                            utcNow,
                            utcNow.Add(_options.EmailOtpLifetime),
                            utcNow.Add(_options.EmailOtpResendCooldown));

                        session.MarkSubmitted(
                            utcNow,
                            utcNow.Add(_options.SubmittedSessionLifetime));

                        await _libraryRepository.AddLibraryAsync(library);
                        await _registrationRepository.AddChallengeAsync(challenge, cancellationToken);

                        try
                        {
                            await _libraryRepository.SaveChangesAsync();
                        }
                        catch (ApplicationBusinessException ex)
                            when (ex.Message == LibraryErrorCodes.DuplicateLibraryForUser
                                  || ex.Message == LibraryErrorCodes.DuplicateLibraryEmail)
                        {
                            throw new DomainException(ex.Message);
                        }

                        databaseCommitted = true;
                    }
                    catch
                    {
                        if (!databaseCommitted)
                        {
                            await DeleteStoredImagesAsync(
                                libraryImagePath,
                                headerImagePath,
                                CancellationToken.None);
                        }

                        throw;
                    }

                    // Persistence deliberately precedes SMTP. The delivery outcome is
                    // part of the committed response, so clients never need to infer
                    // submission state from a provider failure or an HTTP 500.
                    var emailDeliveryStatus = await _emailSender.SendVerificationOtpAsync(
                        library!.Email,
                        library.LibraryName,
                        challenge!.Generation,
                        otpCode!,
                        _options.EmailOtpLifetime,
                        cancellationToken);

                    if (emailDeliveryStatus == EmailDeliveryStatus.NotSent)
                    {
                        await BestEffortCompensateDefiniteNotSentAsync(
                            challenge,
                            challenge.Generation,
                            challenge.ConcurrencyStamp);
                    }

                    return new LibraryRegistrationSubmissionResponse(
                        library.Id,
                        challenge.Generation,
                        library.ApprovalStatus,
                        LibraryEmailMasker.Mask(library.Email),
                        emailDeliveryStatus,
                        challenge.ExpiresAtUtc,
                        challenge.ResendAvailableAtUtc,
                        session.ExpiresAtUtc);
                },
                "Library registration submitted; email verification is required.");
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
                // The application and challenge are already durable. A concurrent
                // resend/verification must win over stale compensation, and a failed
                // compensation must not turn the committed submission into a 500.
                Logger.LogWarning(
                    exception,
                    "Could not compensate definite SMTP non-delivery for challenge {ChallengeId} verification {VerificationId}.",
                    challenge.Id,
                    attemptedGeneration);
            }
        }

        private async Task DeleteStoredImagesAsync(
            string? libraryImagePath,
            string? headerImagePath,
            CancellationToken cancellationToken)
        {
            try
            {
                await _libraryImageStorageService.DeleteAsync(libraryImagePath, cancellationToken);
                await _libraryImageStorageService.DeleteAsync(headerImagePath, cancellationToken);
            }
            catch (Exception exception)
            {
                Logger.LogWarning(
                    exception,
                    "Failed to delete uploaded library images after registration failure.");
            }
        }
    }
}
