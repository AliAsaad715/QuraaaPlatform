using System.Text.Json.Serialization;

namespace Quraaa.API.Requests.Profiles;

public sealed record CreateProfileLocationRequest(
    string Name,
    string? Address,
    [property: JsonRequired] double Latitude,
    [property: JsonRequired] double Longitude,
    bool IsDefault = false);

public sealed record UpdateProfileLocationRequest(
    string Name,
    string? Address,
    [property: JsonRequired] double Latitude,
    [property: JsonRequired] double Longitude);
