using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Configurations
{
    public sealed class ApplicationUserConfiguration
        : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.HasIndex(user => user.RefreshToken)
                .IsUnique()
                .HasDatabaseName("IX_AspNetUsers_RefreshToken")
                .HasFilter("\"RefreshToken\" IS NOT NULL");

            builder.HasIndex(user => user.RefreshTokenFamilyId)
                .IsUnique()
                .HasDatabaseName("IX_AspNetUsers_RefreshTokenFamilyId")
                .HasFilter("\"RefreshTokenFamilyId\" IS NOT NULL");
        }
    }
}
