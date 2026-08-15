using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Domain.Catalog;
using Quraaa.Domain.Reports;
using Quraaa.Domain.User;

namespace Quraaa.Persistence.Configurations
{
    public sealed class BookReportConfiguration : IEntityTypeConfiguration<BookReportAggregate>
    {
        public void Configure(EntityTypeBuilder<BookReportAggregate> builder)
        {
            builder.ToTable("BookReports", table =>
            {
                // A closed report must say who closed it and when; an open one
                // must not pretend it was reviewed.
                table.HasCheckConstraint(
                    "CK_BookReports_Review_Consistent",
                    "(\"Status\" = 1 AND \"ReviewedByAdminId\" IS NULL AND \"ReviewedAtUtc\" IS NULL) OR " +
                    "(\"Status\" IN (2, 3, 4) AND \"ReviewedByAdminId\" IS NOT NULL AND \"ReviewedAtUtc\" IS NOT NULL)");
            });

            builder.HasKey(report => report.Id);
            builder.Property(report => report.Id)
                   .ValueGeneratedNever();

            builder.Property(report => report.UserId)
                   .IsRequired();

            builder.Property(report => report.BookId)
                   .IsRequired();

            builder.Property(report => report.Reason)
                   .HasConversion<int>()
                   .IsRequired();

            builder.Property(report => report.Details)
                   .HasMaxLength(BookReportAggregate.MaxDetailsLength);

            builder.Property(report => report.Status)
                   .HasConversion<int>()
                   .IsRequired();

            builder.Property(report => report.ModeratorNote)
                   .HasMaxLength(BookReportAggregate.MaxModeratorNoteLength);

            builder.Property(report => report.ReviewedByAdminId);
            builder.Property(report => report.ReviewedAtUtc);

            builder.Property(report => report.LastModificationTime)
                   .IsConcurrencyToken();

            // One report per reader per book — the race-proof half of the
            // duplicate guard the handler pre-checks. Filtered on IsDeleted so
            // the index matches that pre-check exactly: a withdrawn report must
            // not block the reader from reporting the book again.
            builder.HasIndex(report => new { report.UserId, report.BookId })
                   .IsUnique()
                   .HasFilter("\"IsDeleted\" = false");

            // Drives the moderation queue (status + newest first) and the
            // per-book / per-reporter views.
            builder.HasIndex(report => new { report.Status, report.CreationTime });
            builder.HasIndex(report => report.BookId);
            builder.HasIndex(report => report.CreationTime);

            builder.HasOne<UserAggregate>()
                   .WithMany()
                   .HasForeignKey(report => report.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<BookAggregate>()
                   .WithMany()
                   .HasForeignKey(report => report.BookId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
