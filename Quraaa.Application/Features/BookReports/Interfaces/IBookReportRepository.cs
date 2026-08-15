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

        Task<BookReportAggregate?> GetByIdAsync(
            Guid reportId,
            CancellationToken cancellationToken = default);

        Task<BookReportResponse?> GetResponseByIdAsync(
            Guid reportId,
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
