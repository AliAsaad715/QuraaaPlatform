using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Configurations
{
    public sealed class OrphanFileCandidateConfiguration : IEntityTypeConfiguration<OrphanFileCandidate>
    {
        public void Configure(EntityTypeBuilder<OrphanFileCandidate> builder)
        {
            builder.ToTable("OrphanFileCandidates");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.Property(x => x.RelativePath)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.DetectedAtUtc).IsRequired();
            builder.Property(x => x.Status).IsRequired();

            // One tracking row per physical path — re-detecting an already-pending
            // file during discovery is a no-op, not a duplicate insert.
            builder.HasIndex(x => x.RelativePath).IsUnique();

            // Drives the deletion-phase query: WHERE Status = Pending AND DetectedAtUtc <= cutoff.
            builder.HasIndex(x => new { x.Status, x.DetectedAtUtc });
        }
    }
}
