using System.Text.Json.Serialization;

namespace Quraaa.Infrastructure.Models
{
    public record GoogleBooksLanguageResponse(
        [property: JsonPropertyName("items")] List<GoogleBooksLanguageItem>? Items
    );

    public record GoogleBooksLanguageItem(
        [property: JsonPropertyName("volumeInfo")] GoogleBooksLanguageVolumeInfo? VolumeInfo
    );

    public record GoogleBooksLanguageVolumeInfo(
        [property: JsonPropertyName("language")] string? Language
    );
}