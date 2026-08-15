using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Domain.Library;

namespace Quraaa.Persistence.Configurations
{
    public sealed class LibraryPasswordResetChallengeConfiguration
        : IEntityTypeConfiguration<LibraryPasswordResetChallenge>
    {
        public void Configure(EntityTypeBuilder<LibraryPasswordResetChallenge> builder)
        {
            builder.ToTable("LibraryPasswordResetChallenges");

            builder.HasKey(challenge => challenge.Id);
            builder.Property(challenge => challenge.Id)
                .ValueGeneratedNever();

            builder.Property(challenge => challenge.LibraryId)
                .IsRequired();

            builder.Property(challenge => challenge.CodeHash)
                .HasMaxLength(128);

            builder.Property(challenge => challenge.Generation)
                .IsRequired();

            builder.Property(challenge => challenge.ExpiresAtUtc)
                .IsRequired();

            builder.Property(challenge => challenge.ResendAvailableAtUtc)
                .IsRequired();

            builder.Property(challenge => challenge.FailedAttemptCount)
                .IsRequired();

            builder.Property(challenge => challenge.LockedUntilUtc);
            builder.Property(challenge => challenge.ConsumedAtUtc);
            builder.Property(challenge => challenge.SendWindowStartedAtUtc);

            builder.Property(challenge => challenge.SendCount)
                .IsRequired();

            builder.Property(challenge => challenge.ConcurrencyStamp)
                .IsRequired()
                .IsConcurrencyToken();

            builder.HasOne<LibraryAggregate>()
                .WithOne()
                .HasForeignKey<LibraryPasswordResetChallenge>(challenge => challenge.LibraryId)
                .OnDelete(DeleteBehavior.Cascade);

            // One reset challenge per library, reused across resets.
            builder.HasIndex(challenge => challenge.LibraryId)
                .IsUnique();
        }
    }
}
