using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Configurations
{
    public sealed class ConsumedRefreshTokenConfiguration
        : IEntityTypeConfiguration<ConsumedRefreshToken>
    {
        public void Configure(EntityTypeBuilder<ConsumedRefreshToken> builder)
        {
            builder.ToTable("ConsumedRefreshTokens");

            builder.HasKey(token => token.Id);
            builder.Property(token => token.Id).ValueGeneratedNever();

            builder.Property(token => token.TokenHash)
                .HasMaxLength(128)
                .IsRequired();

            builder.Property(token => token.ConsumedAtUtc).IsRequired();
            builder.Property(token => token.ExpiresAtUtc).IsRequired();

            builder.HasIndex(token => token.TokenHash)
                .IsUnique();

            builder.HasIndex(token => new { token.UserId, token.FamilyId });
            builder.HasIndex(token => token.ExpiresAtUtc);

            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
