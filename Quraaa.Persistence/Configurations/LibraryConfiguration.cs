using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Domain.Library;

namespace Quraaa.Persistence.Configurations
{
       public class LibraryConfiguration : IEntityTypeConfiguration<LibraryAggregate>
       {
              public void Configure(EntityTypeBuilder<LibraryAggregate> builder)
              {
                     builder.ToTable("Libraries");

                     builder.HasKey(l => l.Id);
                     builder.Property(l => l.Id)
                            .ValueGeneratedNever();

                     builder.Property(l => l.LibraryName)
                            .HasMaxLength(100)
                            .IsRequired();

                     builder.Property(l => l.Location)
                            .HasMaxLength(250)
                            .IsRequired();

                     builder.Property(l => l.LibraryImage)
                            .HasMaxLength(500)
                            .IsRequired();

                     builder.Property(l => l.HeaderImage)
                            .HasMaxLength(500)
                            .IsRequired();

                     builder.Property(l => l.Email)
                            .HasMaxLength(256)
                            .IsRequired();

                     builder.Property(l => l.UserId)
                            .IsRequired();

                     builder.Property(l => l.ApprovalStatus)
                            .IsRequired();

                     builder.HasIndex(l => l.UserId)
                            .IsUnique();
                     builder.HasIndex(l => l.Email);
              }
       }
}
