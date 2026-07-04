using System.Text.Json.Serialization;

namespace Quraaa.Infrastructure.Models
{
    public record GoogleBooksResponse(
        [property: JsonPropertyName("items")] List<GoogleBookItem>? Items
    );

    public record GoogleBookItem(
        [property: JsonPropertyName("volumeInfo")] VolumeInfo VolumeInfo
    );

    public record VolumeInfo(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("authors")] List<string>? Authors,
        [property: JsonPropertyName("publisher")] string? Publisher,
        [property: JsonPropertyName("publishedDate")] string? PublishedDate,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("imageLinks")] ImageLinks? ImageLinks,
        [property: JsonPropertyName("language")] string? Language
    );

    public record ImageLinks(
        [property: JsonPropertyName("thumbnail")] string? Thumbnail
    );
}
