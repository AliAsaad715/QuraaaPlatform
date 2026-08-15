using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Payouts.Common;
using Quraaa.Application.Features.Payouts.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Payouts.Commands.CreateLibraryWalletDashboardLink
{
    public class CreateLibraryWalletDashboardLinkCommandHandler
        : BaseApplicationService<CreateLibraryWalletDashboardLinkCommandHandler>,
          IRequestHandler<CreateLibraryWalletDashboardLinkCommand, AppResult<LibraryWalletDashboardLinkResponse>>
    {
        private readonly ILibraryRepository _libraryRepository;
        private readonly IPayoutGateway _payoutGateway;

        public CreateLibraryWalletDashboardLinkCommandHandler(
            ILibraryRepository libraryRepository,
            IPayoutGateway payoutGateway,
            ILogger<CreateLibraryWalletDashboardLinkCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
            _payoutGateway = payoutGateway;
        }

        public async Task<AppResult<LibraryWalletDashboardLinkResponse>> Handle(
            CreateLibraryWalletDashboardLinkCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<CreateLibraryWalletDashboardLinkCommand, LibraryWalletDashboardLinkResponse>(request, async () =>
            {
                var library = await _libraryRepository.GetApprovedByUserIdAsync(
                    request.UserId,
                    cancellationToken);

                if (library is null)
                {
                    throw new NotFoundException("Library not found");
                }

                if (library.StripeConnectAccountId is null)
                {
                    throw new ApplicationBusinessException(
                        "The library has no Stripe wallet yet. Start the wallet onboarding first.",
                        "Wallet");
                }

                // The Express dashboard exposes bank details and identity data.
                // Only issue a login link for an account the platform itself
                // created for THIS library (provenance in Stripe metadata) —
                // never for an account attached by id.
                var account = await _payoutGateway.GetConnectedAccountAsync(
                    library.StripeConnectAccountId,
                    cancellationToken);

                if (account is null || account.OwnerLibraryId != library.Id)
                {
                    throw new ApplicationBusinessException(
                        "A hosted dashboard link is only available for wallets created through the in-app Stripe onboarding. Manage this account at dashboard.stripe.com instead.",
                        "Wallet");
                }

                var url = await _payoutGateway.CreateExpressDashboardLinkAsync(
                    library.StripeConnectAccountId,
                    cancellationToken);

                if (url is null)
                {
                    throw new ApplicationBusinessException(
                        "This Stripe account type does not support a hosted dashboard link. Manage it at dashboard.stripe.com instead.",
                        "Wallet");
                }

                return new LibraryWalletDashboardLinkResponse(url);
            }, "Stripe dashboard link created");
        }
    }
}
