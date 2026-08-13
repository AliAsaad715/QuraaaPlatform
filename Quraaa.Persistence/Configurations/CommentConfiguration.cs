using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Domain.Catalog;
using Quraaa.Domain.Comments;
using Quraaa.Domain.User;

namespace Quraaa.Persistence.Configurations
{
    public class CommentConfiguration : IEntityTypeConfiguration<CommentAggregate>
    {
        public void Configure(EntityTypeBuilder<CommentAggregate> builder)
        {
            builder.ToTable("Comments");

            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id)
                   .ValueGeneratedNever();

            builder.Property(c => c.UserId)
                   .IsRequired();

            builder.Property(c => c.BookId)
                   .IsRequired();

            builder.Property(c => c.Content)
                   .HasMaxLength(2000)
                   .IsRequired();

            builder.HasIndex(c => c.BookId);
            builder.HasIndex(c => c.UserId);
            builder.HasIndex(c => c.CreationTime);

            builder.HasOne<UserAggregate>()
                   .WithMany()
                   .HasForeignKey(c => c.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<BookAggregate>()
                   .WithMany()
                   .HasForeignKey(c => c.BookId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
