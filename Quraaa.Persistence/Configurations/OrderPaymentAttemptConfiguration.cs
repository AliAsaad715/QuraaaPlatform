using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Domain.Orders.Entities;

namespace Quraaa.Persistence.Configurations
{
    public sealed class OrderPaymentAttemptConfiguration
        : IEntityTypeConfiguration<PaymentAttempt>
    {
        public void Configure(EntityTypeBuilder<PaymentAttempt> builder)
        {
            builder.ToTable("OrderPaymentAttempts", table =>
            {
                table.HasCheckConstraint(
                    "CK_OrderPaymentAttempts_AttemptNumber_Positive",
                    "\"AttemptNumber\" > 0");

                table.HasCheckConstraint(
                    "CK_OrderPaymentAttempts_AmountMinor_Positive",
                    "\"AmountMinor\" > 0");

                table.HasCheckConstraint(
                    "CK_OrderPaymentAttempts_CheckoutFields_Paired",
                    "(\"CheckoutSessionId\" IS NULL AND \"CheckoutUrl\" IS NULL) OR " +
                    "(\"CheckoutSessionId\" IS NOT NULL AND \"CheckoutUrl\" IS NOT NULL)");
            });

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.Property(x => x.OrderId).IsRequired();
            builder.Property(x => x.AttemptNumber).IsRequired();

            builder.Property(x => x.Provider)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.AmountMinor).IsRequired();

            builder.Property(x => x.Currency)
                .HasMaxLength(3)
                .IsRequired();

            builder.Property(x => x.SuccessUrl)
                .HasMaxLength(2048)
                .IsRequired();

            builder.Property(x => x.CancelUrl)
                .HasMaxLength(2048)
                .IsRequired();

            builder.Property(x => x.ExpiresAtUtc)
                .IsRequired();

            builder.Property(x => x.CheckoutSessionId)
                .HasMaxLength(255);

            builder.Property(x => x.CheckoutUrl)
                .HasMaxLength(2048);

            builder.Property(x => x.PaymentIntentId)
                .HasMaxLength(255);

            builder.Property(x => x.FailureCode)
                .HasMaxLength(100);

            builder.Property(x => x.FailureMessage)
                .HasMaxLength(1000);

            builder.HasIndex(x => new { x.OrderId, x.AttemptNumber })
                .IsUnique();

            builder.HasIndex(x => x.CheckoutSessionId)
                .IsUnique()
                .HasFilter("\"CheckoutSessionId\" IS NOT NULL");

            builder.HasIndex(x => x.PaymentIntentId)
                .IsUnique()
                .HasFilter("\"PaymentIntentId\" IS NOT NULL");

            builder.HasIndex(x => new { x.OrderId, x.Status });
            builder.HasIndex(x => x.ExpiresAtUtc);
        }
    }
}
