using MediatR;
using Quraaa.Application.Features.Payouts.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Payouts.Commands.CreateLibraryWalletDashboardLink
{
    /// <summary>
    /// Library owner (JWT): creates a short-lived link to the owner's Stripe
    /// Express dashboard to edit bank details and view Stripe-side payouts.
    /// </summary>
    public record CreateLibraryWalletDashboardLinkCommand(Guid UserId)
        : IRequest<AppResult<LibraryWalletDashboardLinkResponse>>;
}
