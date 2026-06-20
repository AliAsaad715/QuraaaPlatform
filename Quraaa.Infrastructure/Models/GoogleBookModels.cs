namespace Quraaa.Infrastructure.Models
{
    public record GoogleBooksResponse(List<GoogleBookItem>? Items);

    public record GoogleBookItem(VolumeInfo VolumeInfo);

    public record VolumeInfo(
        string? Title,
        List<string>? Authors,
        string? Publisher,
        string? PublishedDate,
        string? Description,
        ImageLinks? ImageLinks
    );

    public record ImageLinks(string? Thumbnail);
}
