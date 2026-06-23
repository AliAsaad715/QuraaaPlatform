namespace Quraaa.Application.Features.Ebooks.Common
{
    public record EbookResponse(
        Guid ListingId,
        Guid BookId,
        string Title,
        string Author,
        string Description,
        string CoverImageUrl,
        Guid CategoryId,
        string Language,
        string? Isbn,
        decimal Price,
        string DigitalAssetUrl);
}
