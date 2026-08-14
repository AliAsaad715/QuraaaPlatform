using Quraaa.Domain.Marketplace.Enums;

namespace Quraaa.API.Requests.Books
{
    public class GetHomePageCatalogRequest
    {
        public string? SearchTerm { get; set; }
        public Guid? CategoryId { get; set; }
        public Guid? LibraryId { get; set; }
        public ListingFormat? Format { get; set; }
        public SellerType? SellerType {  get; set; }
        public bool? IsFree { get; set; }
        public BookCondition? Condition { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string SortBy { get; set; } = "latest";
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
