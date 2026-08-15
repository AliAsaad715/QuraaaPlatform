using System.Text.Json.Serialization;

namespace Quraaa.Domain.Catalog.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Language
{
    Arabic = 1,
    English = 2,
    French = 3,
    Other = 99
}
