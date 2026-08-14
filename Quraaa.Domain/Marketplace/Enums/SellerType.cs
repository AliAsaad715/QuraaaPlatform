using System.Text.Json.Serialization;

namespace Quraaa.Domain.Marketplace.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SellerType
    {
        Library = 1,
        User = 2,
    }
}