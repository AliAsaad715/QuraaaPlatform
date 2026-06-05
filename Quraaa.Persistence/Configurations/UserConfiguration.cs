using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Domain.User;
using Quraaa.Persistence.Data;
using System.Text.Json;

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

            builder.Property(u => u.Interests)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null!) ?? new List<string>()
                )
                .HasColumnName("Interests")
                .HasColumnType("nvarchar(max)");

            builder.Metadata
                .FindProperty(nameof(UserAggregate.Interests))?
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            builder.OwnsOne(u => u.PaymentMethod, pb =>
            {
                pb.Property(p => p.GatewayCustomerId).HasMaxLength(100).HasColumnName("PaymentCustomerId");
                pb.Property(p => p.CardBrand).HasMaxLength(20).HasColumnName("PaymentCardBrand");
                pb.Property(p => p.LastFourDigits).HasMaxLength(4).HasColumnName("PaymentLastFourDigits");
            });
        }
    }
}
