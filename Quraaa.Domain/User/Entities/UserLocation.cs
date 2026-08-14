using Quraaa.Domain.Shared.Entities;
using Quraaa.Domain.Shared.Exceptions;
using Quraaa.Domain.User.ValueObjects;

namespace Quraaa.Domain.User.Entities;

public sealed class UserLocation : AuditableEntity
{
    public const int NameMaxLength = 100;
    public const int AddressMaxLength = 250;

    public Guid UserId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Address { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }

    private UserLocation() { }

    private UserLocation(
        Guid id,
        Guid userId,
        string name,
        string? address,
        double latitude,
        double longitude)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        Id = id;
        UserId = userId;
        SetDetails(name, address, latitude, longitude);
    }

    internal static UserLocation Create(
        Guid userId,
        string name,
        string? address,
        double latitude,
        double longitude) =>
        new(Guid.NewGuid(), userId, name, address, latitude, longitude);

    internal void Update(
        string name,
        string? address,
        double latitude,
        double longitude)
    {
        SetDetails(name, address, latitude, longitude);
        UpdateModificationTime();
    }

    private void SetDetails(
        string name,
        string? address,
        double latitude,
        double longitude)
    {
        var normalizedName = name?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new DomainException("Location name is required.");
        }

        if (normalizedName.Length > NameMaxLength)
        {
            throw new DomainException($"Location name cannot exceed {NameMaxLength} characters.");
        }

        var normalizedAddress = string.IsNullOrWhiteSpace(address)
            ? null
            : address.Trim();

        if (normalizedAddress?.Length > AddressMaxLength)
        {
            throw new DomainException($"Location address cannot exceed {AddressMaxLength} characters.");
        }

        var coordinates = new GeoLocation(latitude, longitude);

        Name = normalizedName;
        Address = normalizedAddress;
        Latitude = coordinates.Latitude;
        Longitude = coordinates.Longitude;
    }
}
