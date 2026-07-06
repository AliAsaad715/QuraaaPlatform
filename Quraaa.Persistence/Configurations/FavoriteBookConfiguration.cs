using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Domain.Catalog;
using Quraaa.Domain.Favorites;
using Quraaa.Domain.User;

namespace Quraaa.Persistence.Configurations
{
    public class FavoriteBookConfiguration : IEntityTypeConfiguration<FavoriteBookAggregate>
    {
        public void Configure(EntityTypeBuilder<FavoriteBookAggregate> builder)
        {
            builder.ToTable("FavoriteBooks");

            builder.HasKey(f => f.Id);
            builder.Property(f => f.Id)
                   .ValueGeneratedNever();

            builder.Property(f => f.UserId)
                   .IsRequired();

            builder.Property(f => f.BookId)
                   .IsRequired();

            builder.HasIndex(f => f.UserId);
            builder.HasIndex(f => f.BookId);

            builder.HasOne<UserAggregate>()
                   .WithMany()
                   .HasForeignKey(f => f.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<BookAggregate>()
                   .WithMany()
                   .HasForeignKey(f => f.BookId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(f => new { f.UserId, f.BookId })
                   .IsUnique()
                   .HasFilter("\"IsDeleted\" = false");
        }
    }
}
