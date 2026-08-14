using System.Text.Json.Serialization;

namespace Quraaa.Domain.Marketplace.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BookCondition
    {
        New = 1,
        LikeNew = 2,
        Good = 3,
        Acceptable = 4,
    }
}