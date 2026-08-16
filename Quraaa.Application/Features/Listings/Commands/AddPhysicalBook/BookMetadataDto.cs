namespace Quraaa.Application.Features.Listings.Commands.AddPhysicalBook
{
    public record BookMetadataDto(
        string Title,
        string Authors,
        string Description,
        string ThumbnailUrl,
        string Publisher,
        string PublishedDate,
        string Language,
        int? PageCount = null
    );
}
