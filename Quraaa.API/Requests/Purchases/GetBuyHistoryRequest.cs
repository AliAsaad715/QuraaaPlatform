using Quraaa.Application.Shared.Requests;

namespace Quraaa.API.Requests.Purchases
{
    public record GetBuyHistoryRequest : PaginationRequestDTO
    {
        public string? SearchTerm { get; init; }
    }
}