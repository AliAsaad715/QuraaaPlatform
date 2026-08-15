using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Domain.Library;
using Quraaa.Domain.User;

namespace Quraaa.Persistence.Configurations
{
       public class LibraryConfiguration : IEntityTypeConfiguration<LibraryAggregate>
       {
              public void Configure(EntityTypeBuilder<LibraryAggregate> builder)
              {
                     builder.ToTable("Libraries", table =>
                     {
                            table.HasCheckConstraint(
                                   "CK_Libraries_ApprovalStatus_EmailVerification",
                                   "\"ApprovalStatus\" = 4 OR (\"ApprovalStatus\" IN (1, 2, 3) AND \"EmailVerifiedAtUtc\" IS NOT NULL)");

                            table.HasCheckConstraint(
                                   "CK_Libraries_ProfitSharePercent_Range",
                                   "\"ProfitSharePercent\" >= 0 AND \"ProfitSharePercent\" <= 100");
                     });

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

                     builder.Property(l => l.EmailVerifiedAtUtc);

                     builder.Property(l => l.PasswordHash)
                            .HasMaxLength(500);

                     builder.Property(l => l.StripeConnectAccountId)
                            .HasMaxLength(255);

                     builder.Property(l => l.StripeWalletActivatedAtUtc);

                     builder.Property(l => l.ProfitSharePercent)
                            .HasColumnType("decimal(7,4)")
                            .IsRequired();

                     builder.Property(l => l.ConcurrencyStamp)
                            .IsRequired()
                            .IsConcurrencyToken();

                     builder.HasOne<UserAggregate>()
                            .WithOne()
                            .HasForeignKey<LibraryAggregate>(l => l.UserId)
                            .OnDelete(DeleteBehavior.Restrict);

                     builder.HasIndex(l => l.UserId)
                            .IsUnique();
                     builder.HasIndex(l => l.Email)
                            .IsUnique();
              }
       }
}
