using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Configurations
{
    public sealed class ProcessedPaymentEventConfiguration
        : IEntityTypeConfiguration<ProcessedPaymentEvent>
    {
        public void Configure(EntityTypeBuilder<ProcessedPaymentEvent> builder)
        {
            builder.ToTable("ProcessedPaymentEvents");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.Property(x => x.Provider)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.EventId)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(x => x.EventType)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.ProcessedAtUtc)
                .IsRequired();

            builder.HasIndex(x => new { x.Provider, x.EventId })
                .IsUnique();

            builder.HasIndex(x => x.OrderId);
            builder.HasIndex(x => x.PaymentAttemptId);
            builder.HasIndex(x => x.ProcessedAtUtc);
        }
    }
}
