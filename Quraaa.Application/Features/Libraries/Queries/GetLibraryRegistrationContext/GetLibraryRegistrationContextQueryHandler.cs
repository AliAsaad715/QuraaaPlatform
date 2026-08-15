using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Libraries.Services;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Library.Enums;

namespace Quraaa.Application.Features.Libraries.Queries.GetLibraryRegistrationContext
{
    public sealed class GetLibraryRegistrationContextQueryHandler
        : BaseApplicationService<GetLibraryRegistrationContextQueryHandler>,
          IRequestHandler<GetLibraryRegistrationContextQuery, AppResult<LibraryRegistrationContextResponse>>
    {
        private readonly LibraryRegistrationSessionService _sessionService;
        private readonly ILibraryRepository _libraryRepository;
        private readonly ILibraryRegistrationRepository _registrationRepository;

        public GetLibraryRegistrationContextQueryHandler(
            LibraryRegistrationSessionService sessionService,
            ILibraryRepository libraryRepository,
            ILibraryRegistrationRepository registrationRepository,
            ILogger<GetLibraryRegistrationContextQueryHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _sessionService = sessionService;
            _libraryRepository = libraryRepository;
            _registrationRepository = registrationRepository;
        }

        public async Task<AppResult<LibraryRegistrationContextResponse>> Handle(
            GetLibraryRegistrationContextQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<GetLibraryRegistrationContextQuery, LibraryRegistrationContextResponse>(
                request,
                async () =>
                {
                    // allowCompleted: a finished wizard still answers with its
                    // Completed stage so the dashboard can render a summary.
                    var session = await _sessionService.ResolveActiveAsync(
                        request.Token,
                        requireSubmitted: false,
                        cancellationToken,
                        allowCompleted: true);
                    var library = await _libraryRepository.GetByUserIdAsync(
                        session.UserId,
                        cancellationToken);

                    if (library is null)
                    {
                        if (session.SubmittedAtUtc.HasValue)
                        {
                            throw new UnauthenticatedException();
                        }

                        return new LibraryRegistrationContextResponse(
                            LibraryRegistrationStage.DetailsRequired,
                            session.ExpiresAtUtc,
                            null,
                            null,
                            null,
                            null,
                            null);
                    }

                    if (!session.SubmittedAtUtc.HasValue)
                    {
                        throw new UnauthenticatedException();
                    }

                    if (library.EmailVerifiedAtUtc.HasValue)
                    {
                        // Email done. Either the Stripe wallet step is open, or
                        // the wizard is complete (wallet active / session closed).
                        var stage = session.CompletedAtUtc.HasValue
                            || library.IsStripeWalletActive
                            || library.ApprovalStatus == LibraryApprovalStatus.Rejected
                            ? LibraryRegistrationStage.Completed
                            : LibraryRegistrationStage.StripeWalletSetup;

                        return new LibraryRegistrationContextResponse(
                            stage,
                            session.ExpiresAtUtc,
                            library.Id,
                            null,
                            LibraryEmailMasker.Mask(library.Email),
                            null,
                            null,
                            library.WalletStatus,
                            library.StripeConnectAccountId);
                    }

                    var challenge = await _registrationRepository.GetChallengeByLibraryIdAsync(
                        library.Id,
                        cancellationToken);

                    return new LibraryRegistrationContextResponse(
                        LibraryRegistrationStage.EmailVerificationRequired,
                        session.ExpiresAtUtc,
                        library.Id,
                        challenge?.Generation,
                        LibraryEmailMasker.Mask(library.Email),
                        challenge?.CodeHash is null ? null : challenge.ExpiresAtUtc,
                        challenge?.ResendAvailableAtUtc);
                },
                "Library registration context retrieved successfully.");
        }
    }
}
