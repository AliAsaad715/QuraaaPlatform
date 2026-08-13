using Quraaa.Domain.Shared.Entities;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Domain.User.ValueObjects;

public sealed class GeoLocation : ValueObjectRoot
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }

    private GeoLocation() { }

    public GeoLocation(double latitude, double longitude)
    {
        if (!double.IsFinite(latitude) || latitude is < -90 or > 90)
            throw new DomainException("Latitude must be a finite number between -90 and 90.");
        if (!double.IsFinite(longitude) || longitude is < -180 or > 180)
            throw new DomainException("Longitude must be a finite number between -180 and 180.");

        Latitude = latitude;
        Longitude = longitude;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
    }
}
