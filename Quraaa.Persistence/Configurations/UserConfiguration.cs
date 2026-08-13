using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Domain.Category;
using Quraaa.Domain.User;
using Quraaa.Domain.User.Entities;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<UserAggregate>
    {
        public void Configure(EntityTypeBuilder<UserAggregate> builder)
        {
            builder.ToTable("UsersProfiles");

            builder.HasKey(u => u.Id);
            builder.Property(u => u.Id)
                   .ValueGeneratedNever();

            builder.HasOne<ApplicationUser>()
                   .WithOne()
                   .HasForeignKey<UserAggregate>(u => u.Id)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Property(u => u.FirstName)
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(u => u.LastName)
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(u => u.PhoneNumber)
                   .HasMaxLength(20)
                   .IsRequired();

            builder.Property(u => u.DateOfBirth)
                   .IsRequired();

            builder.Property(u => u.DefaultLocationId);

            builder.Property(u => u.LocationConcurrencyStamp)
                   .IsConcurrencyToken();

            builder.OwnsMany(u => u.Interests, ib =>
            {
                ib.ToTable("UserInterests");

                ib.HasKey(i => i.Id);
                ib.Property(i => i.Id).ValueGeneratedNever();

                ib.WithOwner().HasForeignKey(i => i.UserId);

                ib.Property(i => i.UserId).IsRequired();
                ib.Property(i => i.CategoryId).IsRequired();
                ib.Property(i => i.CreatedAt).IsRequired();

                ib.HasOne<CategoryAggregate>()
                  .WithMany()
                  .HasForeignKey(i => i.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);

                ib.HasIndex(i => new { i.UserId, i.CategoryId }).IsUnique();
            });

            builder.Metadata
                .FindNavigation(nameof(UserAggregate.Interests))?
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            builder.HasMany(u => u.Locations)
                   .WithOne()
                   .HasForeignKey(location => location.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Metadata
                .FindNavigation(nameof(UserAggregate.Locations))?
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            // The FK supplies referential cleanup (SET NULL). The location migration
            // also installs an ownership trigger; a composite SET NULL FK would try
            // to null the non-null UsersProfiles.Id column on location deletion.
            builder.HasOne<UserLocation>()
                   .WithMany()
                   .HasForeignKey(u => u.DefaultLocationId)
                   .OnDelete(DeleteBehavior.SetNull)
                   .HasConstraintName("FK_UsersProfiles_UserLocations_DefaultLocationId");

            builder.OwnsOne(u => u.PaymentMethod, pb =>
            {
                pb.Property(p => p.GatewayCustomerId).HasMaxLength(100).HasColumnName("PaymentCustomerId");
                pb.Property(p => p.CardBrand).HasMaxLength(20).HasColumnName("PaymentCardBrand");
                pb.Property(p => p.LastFourDigits).HasMaxLength(4).HasColumnName("PaymentLastFourDigits");
            });

        }
    }
}
