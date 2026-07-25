using Quraaa.Application.Shared.Requests;

namespace Quraaa.API.Requests.Purchases
{
    public record GetSellHistoryRequest : PaginationRequestDTO
    {
        public string? SearchTerm { get; init; }
    }
}