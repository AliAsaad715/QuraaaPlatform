using System.Text.Json.Serialization;

namespace Quraaa.Infrastructure.Models
{
    public record OpenLibraryBookData(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("subtitle")] string? Subtitle,
        [property: JsonPropertyName("authors")] List<OpenLibraryAuthor>? Authors,
        [property: JsonPropertyName("publishers")] List<OpenLibraryPublisher>? Publishers,
        [property: JsonPropertyName("publish_date")] string? PublishDate,
        [property: JsonPropertyName("cover")] OpenLibraryCover? Cover
    );

    public record OpenLibraryAuthor(
        [property: JsonPropertyName("name")] string? Name
    );

    public record OpenLibraryPublisher(
        [property: JsonPropertyName("name")] string? Name
    );

    public record OpenLibraryLanguage(
        [property: JsonPropertyName("key")] string? Key
    );

    public record OpenLibraryCover(
        [property: JsonPropertyName("small")] string? Small,
        [property: JsonPropertyName("medium")] string? Medium,
        [property: JsonPropertyName("large")] string? Large
    );
}