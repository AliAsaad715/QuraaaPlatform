using MediatR;
using Quraaa.Application.Features.Payouts.Common;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Payouts.Commands.SetLibraryProfitShare
{
    /// <summary>
    /// Administrator command: sets the percentage of a library's gross sales
    /// that is paid out to the library owner on every paid order.
    /// </summary>
    public record SetLibraryProfitShareCommand : IRequest<AppResult<LibraryProfitShareResponse>>
    {
        [JsonIgnore]
        public Guid LibraryId { get; init; }

        [JsonIgnore]
        public Guid AdminId { get; init; }

        /// <summary>
        /// The library owner's share of gross sales, in percent (0–100, up to
        /// four decimal places). Example: <c>12.5</c> means the owner receives
        /// 12.5% of each sale and the platform keeps 87.5%.
        /// </summary>
        public required decimal ProfitSharePercent { get; init; }
    }
}
