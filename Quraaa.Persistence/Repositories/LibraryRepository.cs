using Microsoft.EntityFrameworkCore;
using Npgsql;
using Quraaa.Application.Features.Catalog.Common;
using Quraaa.Application.Features.Categories.Common;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Listings.Queries.GetLibraryBooks;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Domain.Catalog;
using Quraaa.Domain.Category;
using Quraaa.Domain.Library;
using Quraaa.Domain.Library.Enums;
using Quraaa.Domain.Marketplace;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories
{
    public class LibraryRepository : ILibraryRepository
    {
        private readonly ApplicationDbContext _context;

        public LibraryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ExistsByUserIdAsync(Guid userId) =>
            await _context.Libraries.AnyAsync(l => l.UserId == userId);

        public async Task<bool> ExistsByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            var normalizedEmail = NormalizeEmail(email);

            return await _context.Libraries
                .AsNoTracking()
                .AnyAsync(l => l.Email.ToLower() == normalizedEmail, cancellationToken);
        }

        public async Task<bool> ExistsApprovedByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            await _context.Libraries.AnyAsync(
                l => l.UserId == userId && l.ApprovalStatus == LibraryApprovalStatus.Approved,
                cancellationToken);

        public async Task<LibraryAggregate?> GetApprovedByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            var normalizedEmail = NormalizeEmail(email);

            return await _context.Libraries
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    l => l.ApprovalStatus == LibraryApprovalStatus.Approved
                        && l.Email.ToLower() == normalizedEmail,
                    cancellationToken);
        }

        public async Task<bool> ExistsByIdAsync(
            Guid libraryId,
            CancellationToken cancellationToken = default) =>
            await _context.Libraries.AnyAsync(l => l.Id == libraryId, cancellationToken);

        public async Task<LibraryAggregate?> GetByIdAsync(
            Guid libraryId,
            CancellationToken cancellationToken = default) =>
            await _context.Libraries
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    l => l.Id == libraryId && !l.IsDeleted,
                    cancellationToken);

        public async Task AddLibraryAsync(LibraryAggregate library) =>
            await _context.Libraries.AddAsync(library);

        public async Task<(IReadOnlyCollection<LibraryAggregate> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Libraries
                .AsNoTracking()
                .Where(l => l.ApprovalStatus == LibraryApprovalStatus.Approved);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var normalized = searchTerm.Trim();
                query = query.Where(l =>
                    EF.Functions.ILike(l.LibraryName, $"%{normalized}%") ||
                    EF.Functions.ILike(l.Location, $"%{normalized}%"));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(l => l.LibraryName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<(IReadOnlyCollection<ListingSummaryResponse> Items, int TotalCount)> GetLibraryBooksAsync(
            Guid libraryId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            CancellationToken cancellationToken = default)
            {
                var query = _context.Listings
                    .AsNoTracking()
                    .Where(lb => lb.LibraryId == libraryId && lb.Status == ListingStatus.Active)
                    .Join(
                        _context.Books.AsNoTracking(),
                        lb => lb.BookId,
                        b => b.Id,
                        (lb, b) => new { Listing = lb, Book = b })
                    .GroupJoin(
                        _context.Categories.AsNoTracking(),
                        x => x.Book.CategoryId,
                        c => c.Id,
                        (x, categories) => new { x.Listing, x.Book, Categories = categories })
                    .SelectMany(
                        x => x.Categories.DefaultIfEmpty(),
                        (x, c) => new LibraryBookFlatProjection
                        {
                            Listing = x.Listing,
                            Book = x.Book,
                            Category = c
                        });

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var normalized = searchTerm.Trim();
                    query = query.Where(x =>
                        EF.Functions.ILike(x.Book.Title, $"%{normalized}%") ||
                        EF.Functions.ILike(x.Book.Author, $"%{normalized}%") ||
                        EF.Functions.ILike(x.Book.Language, $"%{normalized}%"));
                }

                query = ApplySorting(query, sortBy, sortDescending);

                var totalCount = await query.CountAsync(cancellationToken);

                var items = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(x => new ListingSummaryResponse(
                        x.Listing.Id,
                        x.Listing.Price,
                        x.Listing.Stock,
                        x.Listing.Condition,
                        new BookDetails(
                            x.Book.Id,
                            x.Book.Title,
                            x.Book.Author,
                            x.Book.Description,
                            x.Book.CoverImageUrl,
                            x.Book.Language,
                            x.Book.Isbn,
                            x.Category == null
                                ? null
                                : new CategoryResponse(x.Category.Id, x.Category.NameEn, x.Category.NameAr))))
                    .ToListAsync(cancellationToken);

                return (items, totalCount);
            }

        public async Task<LibraryAggregate?> GetByUserIdAsync(
                Guid userId, CancellationToken cancellationToken = default) =>
                await _context.Libraries
                    .FirstOrDefaultAsync(
                        l => l.UserId == userId &&
                                l.ApprovalStatus == LibraryApprovalStatus.Approved,
                        cancellationToken);

        public async Task SaveChangesAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsDuplicateLibraryForUserViolation(ex))
            {
                throw new ApplicationBusinessException(LibraryErrorCodes.DuplicateLibraryForUser);
            }
            catch (DbUpdateException ex) when (IsDuplicateLibraryEmailViolation(ex))
            {
                throw new ApplicationBusinessException(LibraryErrorCodes.DuplicateLibraryEmail);
            }
        }

        private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

        private static IQueryable<LibraryBookFlatProjection> ApplySorting(
            IQueryable<LibraryBookFlatProjection> query,
            string? sortBy,
            bool sortDescending)
        {
            return sortBy?.ToLowerInvariant() switch
            {
                "author" => sortDescending ? query.OrderByDescending(x => x.Book.Author) : query.OrderBy(x => x.Book.Author),
                "quantity" => sortDescending ? query.OrderByDescending(x => x.Listing.Stock) : query.OrderBy(x => x.Listing.Stock),
                _ => sortDescending ? query.OrderByDescending(x => x.Book.Title) : query.OrderBy(x => x.Book.Title),
            };
        }

        private static bool IsDuplicateLibraryForUserViolation(DbUpdateException exception) =>
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_Libraries_UserId" };

        private static bool IsDuplicateLibraryEmailViolation(DbUpdateException exception) =>
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_Libraries_Email" };

        private sealed class LibraryBookFlatProjection
        {
            public required ListingAggregate Listing { get; set; }
            public required BookAggregate Book { get; set; }
            public CategoryAggregate? Category { get; set; }
        }
    }
}
