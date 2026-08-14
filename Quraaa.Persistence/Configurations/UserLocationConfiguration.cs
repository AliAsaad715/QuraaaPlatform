using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Domain.User.Entities;

namespace Quraaa.Persistence.Configurations;

public sealed class UserLocationConfiguration : IEntityTypeConfiguration<UserLocation>
{
    public void Configure(EntityTypeBuilder<UserLocation> builder)
    {
        builder.ToTable("UserLocations", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_UserLocations_Name_NotBlank",
                "btrim(\"Name\") <> ''");
            tableBuilder.HasCheckConstraint(
                "CK_UserLocations_Latitude_Valid",
                "\"Latitude\" BETWEEN -90 AND 90");
            tableBuilder.HasCheckConstraint(
                "CK_UserLocations_Longitude_Valid",
                "\"Longitude\" BETWEEN -180 AND 180");
        });

        builder.HasKey(location => location.Id);
        builder.Property(location => location.Id).ValueGeneratedNever();

        builder.Property(location => location.UserId).IsRequired();
        builder.Property(location => location.Name)
               .HasMaxLength(UserLocation.NameMaxLength)
               .IsRequired();
        builder.Property(location => location.Address)
               .HasMaxLength(UserLocation.AddressMaxLength);
        builder.Property(location => location.Latitude).IsRequired();
        builder.Property(location => location.Longitude).IsRequired();
        builder.Property(location => location.CreationTime).IsRequired();

        builder.HasIndex(location => location.UserId);
    }
}
