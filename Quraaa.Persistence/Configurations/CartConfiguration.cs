using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Domain.Cart;

namespace Quraaa.Persistence.Configurations
{
    public class CartConfiguration : IEntityTypeConfiguration<CartAggregate>
    {
        public void Configure(EntityTypeBuilder<CartAggregate> builder)
        {
            builder.ToTable("Carts");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.Property(x => x.UserId).IsRequired();
            builder.Property(x => x.Status).HasConversion<int>().IsRequired();
            builder.Property(x => x.StripeCheckoutSessionId).HasMaxLength(200);
            builder.Property(x => x.StripePaymentIntentId).HasMaxLength(200);

            builder.HasIndex(x => new { x.UserId, x.Status });
            builder.HasIndex(x => x.StripeCheckoutSessionId);

            builder.HasMany(x => x.Items)
                .WithOne()
                .HasForeignKey(x => x.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Metadata.FindNavigation(nameof(CartAggregate.Items))
                ?.SetPropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
