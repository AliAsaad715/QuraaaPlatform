namespace Quraaa.API.Requests.Purchases
{
    public record GetBuyHistoryRequest(
        int PageNumber = 1,
        int PageSize = 20,
        string? SearchTerm = null);
}
