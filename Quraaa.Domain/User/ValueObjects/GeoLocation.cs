using Quraaa.Domain.Shared.Entities;
using Quraaa.Domain.Shared.Exceptions;

public class GeoLocation : ValueObjectRoot
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }

    private GeoLocation() { }

    public GeoLocation(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90)
            throw new DomainException("Latitude must be between -90 and 90.");
        if (longitude is < -180 or > 180)
            throw new DomainException("Longitude must be between -180 and 180.");

        Latitude = latitude;
        Longitude = longitude;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
    }
}