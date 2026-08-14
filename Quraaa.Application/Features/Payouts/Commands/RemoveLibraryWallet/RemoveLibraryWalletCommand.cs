using MediatR;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Payouts.Commands.RemoveLibraryWallet
{
    public record RemoveLibraryWalletCommand(Guid UserId) : IRequest<AppResult>;
}
