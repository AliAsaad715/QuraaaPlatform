using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Domain.Category;

namespace Quraaa.Persistence.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<CategoryAggregate>
    {
        public void Configure(EntityTypeBuilder<CategoryAggregate> builder)
        {
            builder.ToTable("Categories");

            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id)
                   .ValueGeneratedNever();

            builder.Property(c => c.Code)
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(c => c.NameAr)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(c => c.NameEn)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(c => c.IsActive)
                   .IsRequired();

            builder.HasOne<CategoryAggregate>()
                   .WithMany()
                   .HasForeignKey(c => c.ParentCategoryId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(c => c.Code).IsUnique();
            builder.HasIndex(c => c.ParentCategoryId);
        }
    }
}