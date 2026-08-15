using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Domain.Author;

namespace Quraaa.Persistence.Configurations
{
    public class AuthorConfiguration : IEntityTypeConfiguration<AuthorAggregate>
    {
        public void Configure(EntityTypeBuilder<AuthorAggregate> builder)
        {
            builder.ToTable("Authors");

            builder.HasKey(a => a.Id);
            builder.Property(a => a.Id)
                   .ValueGeneratedNever();

            builder.Property(a => a.Name)
                   .HasMaxLength(150)
                   .IsRequired();

            builder.Property(a => a.Bio)
                   .HasMaxLength(2000)
                   .IsRequired(false);

            builder.Property(a => a.PhotoUrl)
                   .HasMaxLength(500)
                   .IsRequired(false);

            builder.Property(a => a.BirthDate)
                   .IsRequired(false);

            builder.HasIndex(a => a.Name);
        }
    }
}
