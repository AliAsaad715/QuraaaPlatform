using System.Text.Json.Serialization;

namespace Quraaa.Domain.Marketplace.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ListingFormat
    {
        Digital = 1,
        Physical = 2,
    }
}