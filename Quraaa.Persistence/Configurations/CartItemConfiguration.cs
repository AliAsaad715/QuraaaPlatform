using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Domain.Cart.Entities;

namespace Quraaa.Persistence.Configurations
{
    public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
    {
        public void Configure(EntityTypeBuilder<CartItem> builder)
        {
            builder.ToTable("CartItems");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.Property(x => x.CartId).IsRequired();
            builder.Property(x => x.ListingId).IsRequired();
            builder.Property(x => x.Quantity).IsRequired();
            builder.Property(x => x.UnitPriceSnapshot).HasColumnType("decimal(10,2)").IsRequired();

            builder.HasIndex(x => new { x.CartId, x.ListingId }).IsUnique();
        }
    }
}
