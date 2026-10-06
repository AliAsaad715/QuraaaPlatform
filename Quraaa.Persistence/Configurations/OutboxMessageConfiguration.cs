using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Configurations
{
    public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
    {
        public const int MaxLastErrorLength = 2000;

        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.ToTable("OutboxMessages", table =>
            {
                table.HasCheckConstraint(
                    "CK_OutboxMessages_AttemptCount_NonNegative",
                    "\"AttemptCount\" >= 0");
            });

            builder.HasKey(message => message.Id);
            builder.Property(message => message.Id).ValueGeneratedNever();

            builder.Property(message => message.Type)
                .HasMaxLength(300)
                .IsRequired();

            builder.Property(message => message.Payload)
                .HasColumnType("jsonb")
                .IsRequired();

            builder.Property(message => message.OccurredAtUtc).IsRequired();
            builder.Property(message => message.CorrelationId).IsRequired();
            builder.Property(message => message.AttemptCount).IsRequired();
            builder.Property(message => message.NextAttemptAtUtc).IsRequired();

            builder.Property(message => message.LastError)
                .HasMaxLength(MaxLastErrorLength);

            // Claims look only at unfinished messages, so the index stays small as
            // handled ones accumulate.
            builder.HasIndex(message => new { message.NextAttemptAtUtc, message.OccurredAtUtc })
                .HasDatabaseName("IX_OutboxMessages_Pending")
                .HasFilter("\"ProcessedAtUtc\" IS NULL AND \"AbandonedAtUtc\" IS NULL");

            // A claim leases every message of one save at once.
            builder.HasIndex(message => message.CorrelationId);
        }
    }
}
