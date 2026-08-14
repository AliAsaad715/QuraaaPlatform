using MediatR;
using Quraaa.Application.Features.Payouts.Common;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Payouts.Queries.GetLibraryWallet
{
    public record GetLibraryWalletQuery([property: JsonIgnore] Guid UserId)
        : IRequest<AppResult<LibraryWalletResponse>>;
}
