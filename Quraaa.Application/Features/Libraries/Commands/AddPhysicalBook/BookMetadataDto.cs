namespace Quraaa.Application.Features.Libraries.Commands.AddPhysicalBook
{
    public record BookMetadataDto(
        string Title,
        string Authors,
        string Description,
        string ThumbnailUrl,
        string Publisher,
        string PublishedDate
    );
}
