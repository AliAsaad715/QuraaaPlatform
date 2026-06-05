using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Domain.User;
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
        }
    }
}
