using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Library;
using Quraaa.Domain.Library.Enums;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Libraries.Commands.IssueLibraryRegistrationLink
{
    public sealed class IssueLibraryRegistrationLinkCommandHandler
        : BaseApplicationService<IssueLibraryRegistrationLinkCommandHandler>,
          IRequestHandler<IssueLibraryRegistrationLinkCommand, AppResult<LibraryRegistrationLinkResponse>>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILibraryRepository _libraryRepository;
        private readonly ILibraryRegistrationRepository _registrationRepository;
        private readonly ILibraryRegistrationTokenService _tokenService;
        private readonly LibraryRegistrationOptions _options;

        public IssueLibraryRegistrationLinkCommandHandler(
            IUserRepository userRepository,
            ILibraryRepository libraryRepository,
            ILibraryRegistrationRepository registrationRepository,
            ILibraryRegistrationTokenService tokenService,
            LibraryRegistrationOptions options,
            ILogger<IssueLibraryRegistrationLinkCommandHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _userRepository = userRepository;
            _libraryRepository = libraryRepository;
            _registrationRepository = registrationRepository;
            _tokenService = tokenService;
            _options = options;
        }

        public async Task<AppResult<LibraryRegistrationLinkResponse>> Handle(
            IssueLibraryRegistrationLinkCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<IssueLibraryRegistrationLinkCommand, LibraryRegistrationLinkResponse>(
                request,
                async () =>
                {
                    var user = await _userRepository.GetUserByIdAsync(request.UserId);
                    if (user is null)
                    {
                        throw new NotFoundException("User was not found.");
                    }

                    var library = await _libraryRepository.GetByUserIdAsync(
                        request.UserId,
                        cancellationToken);
                    var session = await _registrationRepository.GetSessionByUserIdAsync(
                        request.UserId,
                        cancellationToken);
                    var isResumingEmailVerification = library is not null
                        && !library.EmailVerifiedAtUtc.HasValue
                        && library.ApprovalStatus is LibraryApprovalStatus.AwaitingEmailVerification
                            or LibraryApprovalStatus.Pending;

                    // Email verified but the (optional) Stripe wallet step was
                    // not finished and the library is still awaiting review:
                    // let the owner back into the wizard to connect Stripe.
                    // Sessions that were already completed (registrations finished
                    // before the Stripe step existed) cannot be reissued; those
                    // owners connect Stripe from the dashboard after approval.
                    var isResumingWalletSetup = library is not null
                        && library.EmailVerifiedAtUtc.HasValue
                        && library.ApprovalStatus == LibraryApprovalStatus.Pending
                        && !library.IsStripeWalletActive
                        && (session is null || !session.CompletedAtUtc.HasValue);

                    if (library is not null && !isResumingEmailVerification && !isResumingWalletSetup)
                    {
                        throw new ConflictException(LibraryErrorCodes.DuplicateLibraryForUserMessage);
                    }

                    var isResuming = isResumingEmailVerification || isResumingWalletSetup;

                    var utcNow = DateTime.UtcNow;
                    var token = _tokenService.Generate();
                    var submittedAtUtc = isResuming ? utcNow : (DateTime?)null;

                    // A link that resumes the wallet step can bind the payout
                    // account, so it gets the same short window as the token
                    // issued right after email verification.
                    var lifetime = isResumingWalletSetup
                        ? _options.WalletSetupSessionLifetime
                        : isResuming
                            ? _options.SubmittedSessionLifetime
                            : _options.MagicLinkLifetime;

                    var expiresAtUtc = utcNow.Add(lifetime);

                    if (session is null)
                    {
                        session = new LibraryRegistrationSession(
                            Guid.NewGuid(),
                            request.UserId,
                            request.AuthenticationSessionId,
                            token.TokenHash,
                            expiresAtUtc,
                            submittedAtUtc);

                        await _registrationRepository.AddSessionAsync(session, cancellationToken);
                    }
                    else
                    {
                        session.Reissue(
                            request.AuthenticationSessionId,
                            token.TokenHash,
                            expiresAtUtc,
                            submittedAtUtc);
                    }

                    await _registrationRepository.SaveChangesAsync(cancellationToken);

                    var linkBuilder = new UriBuilder(_options.DashboardRegisterUrl)
                    {
                        Fragment = $"token={Uri.EscapeDataString(token.RawToken)}"
                    };

                    return new LibraryRegistrationLinkResponse(
                        linkBuilder.Uri.AbsoluteUri,
                        session.ExpiresAtUtc);
                },
                "Library registration link issued successfully.");
        }
    }
}
