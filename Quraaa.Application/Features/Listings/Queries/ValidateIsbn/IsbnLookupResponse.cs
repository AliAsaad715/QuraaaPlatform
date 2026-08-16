namespace Quraaa.Application.Features.Listings.Queries.ValidateIsbn
{
    public record IsbnLookupResponse(
        string Isbn,
        string Title,
        string? Author,
        string? Publisher,
        string? PublishedDate,
        string? Description,
        string? CoverImageUrl,
        string? Language,
        int? PageCount);
}
