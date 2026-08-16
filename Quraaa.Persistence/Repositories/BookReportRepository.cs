using Microsoft.EntityFrameworkCore;
using Npgsql;
using Quraaa.Application.Features.BookReports.Common;
using Quraaa.Application.Features.BookReports.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Domain.Author;
using Quraaa.Domain.Catalog;
using Quraaa.Domain.Reports;
using Quraaa.Domain.Reports.Enums;
using Quraaa.Domain.User.Enums;
using Quraaa.Domain.User;
using Quraaa.Domain.Shared.Exceptions;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories
{
    public class BookReportRepository : IBookReportRepository
    {
        private readonly ApplicationDbContext _context;

        public BookReportRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> BookExistsAsync(
            Guid bookId,
            CancellationToken cancellationToken = default)
        {
            // Withheld books are deliberately NOT reportable: a reader cannot
            // see them, so this reads as "no such book".
            return await _context.Books
                .AsNoTracking()
                .AnyAsync(book => book.Id == bookId && !book.IsDeleted, cancellationToken);
        }

        public async Task<bool> ExistsForUserAndBookAsync(
            Guid userId,
            Guid bookId,
            CancellationToken cancellationToken = default)
        {
            return await _context.BookReports
                .AsNoTracking()
                .AnyAsync(
                    report => report.UserId == userId
                        && report.BookId == bookId
                        && !report.IsDeleted,
                    cancellationToken);
        }

        public async Task<int> CountDistinctReportersAsync(
            Guid bookId,
            Guid? includingUserId = null,
            CancellationToken cancellationToken = default)
        {
            // Rejected reports are excluded so dismissed complaints cannot push
            // a book toward being hidden.
            var query = _context.BookReports
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(report => report.BookId == bookId
                    && !report.IsDeleted
                    && report.Status != BookReportStatus.Rejected);

            if (includingUserId.HasValue)
            {
                // The caller's own report is usually still only staged in the
                // change tracker, which no database query can see. Exclude it
                // here and add it back, so the count never lags by one.
                var otherReporterCount = await query
                    .Where(report => report.UserId != includingUserId.Value)
                    .Select(report => report.UserId)
                    .Distinct()
                    .CountAsync(cancellationToken);

                return otherReporterCount + 1;
            }

            return await query
                .Select(report => report.UserId)
                .Distinct()
                .CountAsync(cancellationToken);
        }

        public async Task<IReadOnlyCollection<Guid>> GetListingLibraryOwnerIdsAsync(
            Guid bookId,
            CancellationToken cancellationToken = default)
        {
            // Books are a shared catalogue: every library currently listing this
            // book is a recipient. The owner's user id is what push targets.
            return await (
                from listing in _context.Listings.AsNoTracking()
                join library in _context.Libraries.AsNoTracking()
                    on listing.LibraryId equals library.Id
                where listing.BookId == bookId
                    && !listing.IsDeleted
                    && !library.IsDeleted
                select library.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyCollection<Guid>> GetAdminUserIdsAsync(
            CancellationToken cancellationToken = default)
        {
            return await _context.UsersProfiles
                .AsNoTracking()
                .Where(user => user.Role == Role.SuperAdmin && !user.IsDeleted)
                .Select(user => user.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<BookReportAggregate?> GetByIdAsync(
            Guid reportId,
            CancellationToken cancellationToken = default)
        {
            return await _context.BookReports
                .FirstOrDefaultAsync(
                    report => report.Id == reportId && !report.IsDeleted,
                    cancellationToken);
        }

        public async Task<BookReportResponse?> GetResponseByIdAsync(
            Guid reportId,
            CancellationToken cancellationToken = default)
        {
            return await ProjectedQuery()
                .FirstOrDefaultAsync(report => report.ReportId == reportId, cancellationToken);
        }

        public async Task<(IReadOnlyCollection<BookReportResponse> Items, int TotalCount)> GetPagedAsync(
            BookReportStatus? status,
            Guid? bookId,
            Guid? reporterUserId,
            string? searchTerm,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            // Filters run against the joined entities, before projection, so
            // they translate to a plain WHERE rather than composing over a
            // computed SELECT.
            var query = JoinedQuery();

            if (status.HasValue)
            {
                query = query.Where(row => row.Report.Status == status.Value);
            }

            if (bookId.HasValue)
            {
                query = query.Where(row => row.Report.BookId == bookId.Value);
            }

            if (reporterUserId.HasValue)
            {
                query = query.Where(row => row.Report.UserId == reporterUserId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var normalizedSearchTerm = searchTerm.Trim().ToLower();

                query = query.Where(row =>
                    row.Book.Title.ToLower().Contains(normalizedSearchTerm)
                    || (row.Author != null
                        && row.Author.Name.ToLower().Contains(normalizedSearchTerm))
                    || (row.Report.Details != null
                        && row.Report.Details.ToLower().Contains(normalizedSearchTerm)));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(row => row.Report.CreationTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(row => Project(row))
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<(IReadOnlyCollection<LibraryBookReportResponse> Items, int TotalCount)> GetPagedForLibraryAsync(
            Guid libraryId,
            BookReportStatus? status,
            Guid? bookId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            // Scoped by the library's own listings, so an owner only ever sees
            // reports about books they actually sell. IgnoreQueryFilters keeps
            // withheld books visible here — that is the case the owner has to
            // act on.
            var query =
                from report in _context.BookReports.AsNoTracking().IgnoreQueryFilters()
                join book in _context.Books.AsNoTracking().IgnoreQueryFilters()
                    on report.BookId equals book.Id
                join author in _context.Authors.AsNoTracking()
                    on book.AuthorId equals author.Id into bookAuthors
                from author in bookAuthors.DefaultIfEmpty()
                where !report.IsDeleted
                    && _context.Listings
                        .Any(listing => listing.BookId == report.BookId
                            && listing.LibraryId == libraryId
                            && !listing.IsDeleted)
                select new { Report = report, Book = book, Author = author };

            if (status.HasValue)
            {
                query = query.Where(row => row.Report.Status == status.Value);
            }

            if (bookId.HasValue)
            {
                query = query.Where(row => row.Report.BookId == bookId.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(row => row.Report.CreationTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(row => new LibraryBookReportResponse(
                    row.Report.Id,
                    row.Report.BookId,
                    row.Book.Title,
                    row.Author != null ? row.Author.Name : string.Empty,
                    row.Book.CoverImageUrl,
                    row.Report.Reason,
                    row.Report.Details,
                    row.Report.Status,
                    row.Report.ReviewedAtUtc,
                    row.Report.CreationTime,
                    row.Book.ModerationStatus,
                    row.Book.CurrentVersionNumber))
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task AddAsync(
            BookReportAggregate report,
            CancellationToken cancellationToken = default)
        {
            await _context.BookReports.AddAsync(report, cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException(
                    "This report was updated by someone else. Reload it and retry.");
            }
            catch (DbUpdateException exception) when (IsDuplicateReportViolation(exception))
            {
                foreach (var entry in exception.Entries
                    .Where(entry => entry.Entity is BookReportAggregate))
                {
                    entry.State = EntityState.Detached;
                }

                throw new ApplicationBusinessException(BookReportErrorCodes.DuplicateBookReport);
            }
        }

        /// <summary>
        /// Reports joined to their book (and the book's optional author) and to
        /// the reporter, left unprojected so callers filter on the entities.
        /// </summary>
        private IQueryable<BookReportRow> JoinedQuery()
        {
            // IgnoreQueryFilters so the moderation queue keeps showing reports
            // for books the escalation already withheld.
            return from report in _context.BookReports.AsNoTracking().IgnoreQueryFilters()
                   join book in _context.Books.AsNoTracking().IgnoreQueryFilters()
                       on report.BookId equals book.Id
                   join reporter in _context.UsersProfiles.AsNoTracking()
                       on report.UserId equals reporter.Id
                   // AuthorId is optional, so the author is a left join.
                   join author in _context.Authors.AsNoTracking()
                       on book.AuthorId equals author.Id into bookAuthors
                   from author in bookAuthors.DefaultIfEmpty()
                   where !report.IsDeleted
                   select new BookReportRow
                   {
                       Report = report,
                       Book = book,
                       Reporter = reporter,
                       Author = author,
                   };
        }

        private IQueryable<BookReportResponse> ProjectedQuery()
        {
            return JoinedQuery().Select(row => Project(row));
        }

        /// <summary>
        /// The single shape every read returns.
        /// </summary>
        private static BookReportResponse Project(BookReportRow row) =>
            new(
                row.Report.Id,
                row.Report.BookId,
                row.Book.Title,
                row.Author != null ? row.Author.Name : string.Empty,
                row.Book.CoverImageUrl,
                row.Report.UserId,
                row.Reporter.FirstName + " " + row.Reporter.LastName,
                row.Report.Reason,
                row.Report.Details,
                row.Report.Status,
                row.Report.ModeratorNote,
                row.Report.ReviewedByAdminId,
                row.Report.ReviewedAtUtc,
                row.Report.CreationTime);

        private sealed class BookReportRow
        {
            public required BookReportAggregate Report { get; init; }
            public required BookAggregate Book { get; init; }
            public required UserAggregate Reporter { get; init; }
            public AuthorAggregate? Author { get; init; }
        }

        private static bool IsDuplicateReportViolation(DbUpdateException exception) =>
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_BookReports_UserId_BookId"
            };
    }
}
