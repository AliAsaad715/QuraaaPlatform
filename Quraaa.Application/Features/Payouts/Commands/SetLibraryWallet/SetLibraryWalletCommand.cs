using MediatR;
using Quraaa.Application.Features.Payouts.Common;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Payouts.Commands.SetLibraryWallet
{
    /// <summary>
    /// Adds or replaces the authenticated library owner's Stripe wallet
    /// (a Stripe Connect account id used as the destination for profit-share
    /// transfers).
    /// </summary>
    public record SetLibraryWalletCommand : IRequest<AppResult<LibraryWalletResponse>>
    {
        [JsonIgnore]
        public Guid UserId { get; init; }

        public string StripeAccountId { get; init; } = string.Empty;
    }
}
