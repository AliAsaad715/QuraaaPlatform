using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Configurations
{
    public sealed class UserDeviceTokenConfiguration : IEntityTypeConfiguration<UserDeviceToken>
    {
        public void Configure(EntityTypeBuilder<UserDeviceToken> builder)
        {
            builder.ToTable("UserDeviceTokens");

            builder.HasKey(token => token.Id);
            builder.Property(token => token.Id).ValueGeneratedNever();

            builder.Property(token => token.DeviceToken)
                .HasMaxLength(4096)
                .IsRequired();

            builder.Property(token => token.RegisteredAtUtc).IsRequired();
            builder.Property(token => token.LastSeenAtUtc).IsRequired();

            // One row per physical token — re-registering the same token upserts.
            builder.HasIndex(token => token.DeviceToken).IsUnique();
            builder.HasIndex(token => token.UserId);

            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
