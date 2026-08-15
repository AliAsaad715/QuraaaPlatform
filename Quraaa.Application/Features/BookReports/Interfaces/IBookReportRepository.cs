using Quraaa.Application.Features.BookReports.Common;
using Quraaa.Domain.Reports;
using Quraaa.Domain.Reports.Enums;

namespace Quraaa.Application.Features.BookReports.Interfaces
{
    public interface IBookReportRepository
    {
        Task<bool> BookExistsAsync(Guid bookId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Whether this reader already reported this book. Checked up front for
        /// a clean error; the unique index is what actually guarantees it.
        /// </summary>
        Task<bool> ExistsForUserAndBookAsync(
            Guid userId,
            Guid bookId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// How many DISTINCT users have an open or upheld report against this
        /// book. Rejected reports are excluded, so dismissed complaints never
        /// push a book toward being hidden.
        /// </summary>
        /// <summary>
        /// How many distinct readers have an open (non-rejected) report against
        /// this book. <paramref name="includingUserId"/> is counted even when
        /// their report is still only staged, so escalation sees the report that
        /// triggered it.
        /// </summary>
        Task<int> CountDistinctReportersAsync(
            Guid bookId,
            Guid? includingUserId = null,
            CancellationToken cancellationToken = default);

        /// <summary>The user ids of every library that currently lists this book.</summary>
        Task<IReadOnlyCollection<Guid>> GetListingLibraryOwnerIdsAsync(
            Guid bookId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<Guid>> GetAdminUserIdsAsync(
            CancellationToken cancellationToken = default);

        Task<BookReportAggregate?> GetByIdAsync(
            Guid reportId,
            CancellationToken cancellationToken = default);

        Task<BookReportResponse?> GetResponseByIdAsync(
            Guid reportId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Reports against books this library currently lists. Withheld books
        /// are included on purpose — they are the ones the owner most needs to
        /// see.
        /// </summary>
        Task<(IReadOnlyCollection<LibraryBookReportResponse> Items, int TotalCount)> GetPagedForLibraryAsync(
            Guid libraryId,
            BookReportStatus? status,
            Guid? bookId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<(IReadOnlyCollection<BookReportResponse> Items, int TotalCount)> GetPagedAsync(
            BookReportStatus? status,
            Guid? bookId,
            Guid? reporterUserId,
            string? searchTerm,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task AddAsync(BookReportAggregate report, CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
