using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Domain.Library;
using Quraaa.Domain.User;

namespace Quraaa.Persistence.Configurations
{
    public sealed class LibraryRegistrationSessionConfiguration
        : IEntityTypeConfiguration<LibraryRegistrationSession>
    {
        public void Configure(EntityTypeBuilder<LibraryRegistrationSession> builder)
        {
            builder.ToTable("LibraryRegistrationSessions");

            builder.HasKey(session => session.Id);
            builder.Property(session => session.Id)
                .ValueGeneratedNever();

            builder.Property(session => session.UserId)
                .IsRequired();

            builder.Property(session => session.AuthenticationSessionId)
                .IsRequired();

            builder.Property(session => session.TokenHash)
                .HasMaxLength(128)
                .IsRequired();

            builder.Property(session => session.ExpiresAtUtc)
                .IsRequired();

            builder.Property(session => session.SubmittedAtUtc);
            builder.Property(session => session.CompletedAtUtc);

            builder.Property(session => session.ConcurrencyStamp)
                .IsRequired()
                .IsConcurrencyToken();

            builder.HasOne<UserAggregate>()
                .WithOne()
                .HasForeignKey<LibraryRegistrationSession>(session => session.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(session => session.UserId)
                .IsUnique();

            builder.HasIndex(session => session.TokenHash)
                .IsUnique();
        }
    }
}
