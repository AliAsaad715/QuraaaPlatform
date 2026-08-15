using Microsoft.EntityFrameworkCore;
using Npgsql;
using Quraaa.Application.Features.Catalog.Common;
using Quraaa.Application.Features.Categories.Common;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Libraries.Queries.GetLibraryRequests;
using Quraaa.Application.Features.Listings.Queries.GetLibraryBooks;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Author;
using Quraaa.Domain.Catalog;
using Quraaa.Domain.Category;
using Quraaa.Domain.Library;
using Quraaa.Domain.Library.Enums;
using Quraaa.Domain.Marketplace;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Domain.Shared.Exceptions;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories
{
    public class LibraryRepository : ILibraryRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IImageUrlFormatter _imageUrlFormatter;

        public LibraryRepository(ApplicationDbContext context, IImageUrlFormatter imageUrlFormatter)
        {
            _context = context;
            _imageUrlFormatter = imageUrlFormatter;
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
            ListingStatus? status = null,
            CancellationToken cancellationToken = default)
            {
                var query = _context.Listings
                    .AsNoTracking()
                    .Where(lb => lb.LibraryId == libraryId && (status == null || lb.Status == status))
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
                        (x, c) => new { x.Listing, x.Book, Category = c })
                    .GroupJoin(
                        _context.Authors.AsNoTracking(),
                        x => x.Book.AuthorId,
                        a => a.Id,
                        (x, authors) => new { x.Listing, x.Book, x.Category, Authors = authors })
                    .SelectMany(
                        x => x.Authors.DefaultIfEmpty(),
                        (x, a) => new LibraryBookFlatProjection
                        {
                            Listing = x.Listing,
                            Book = x.Book,
                            Category = x.Category,
                            Author = a
                        });

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var normalized = searchTerm.Trim();
                    query = query.Where(x =>
                        EF.Functions.ILike(x.Book.Title, $"%{normalized}%") ||
                        EF.Functions.ILike(x.Author!.Name, $"%{normalized}%"));
                }

                query = ApplySorting(query, sortBy, sortDescending);

                var totalCount = await query.CountAsync(cancellationToken);

                // Materialize first, then project to the response DTO in memory —
                // IImageUrlFormatter.Format can't be translated into SQL.
                var rows = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                var items = rows
                    .Select(x => new ListingSummaryResponse(
                        x.Listing.Id,
                        x.Listing.Price,
                        x.Listing.Stock,
                        x.Listing.Condition,
                        x.Listing.Status,
                        x.Listing.Version,
                        new BookDetails(
                            x.Book.Id,
                            x.Book.Title,
                            x.Author?.Name,
                            x.Book.Description,
                            _imageUrlFormatter.Format(x.Book.CoverImageUrl),
                            x.Book.Language,
                            x.Book.Isbn,
                            x.Category == null
                                ? null
                                : new CategoryResponse(x.Category.Id, x.Category.NameEn, x.Category.NameAr))))
                    .ToList();

                return (items, totalCount);
            }

        public async Task<LibraryAggregate?> GetApprovedByUserIdAsync(
                Guid userId, CancellationToken cancellationToken = default) =>
                await _context.Libraries
                    .FirstOrDefaultAsync(
                        l => l.UserId == userId &&
                                l.ApprovalStatus == LibraryApprovalStatus.Approved,
                        cancellationToken);

        public async Task<(IReadOnlyCollection<LibraryRequestResponse> Items, int TotalCount)> GetRequestsAsync(
            LibraryApprovalStatus? status,
            int pageNumber,
            int pageSize,
            string? searchTerm,
            CancellationToken cancellationToken = default)
            {
                // Inner join is safe here — LibraryAggregate.UserId is a required,
                // non-nullable Guid (no factory path creates a library without one),
                // unlike the optional Book.CategoryId case from earlier that needed a
                // left join to avoid silently dropping rows.
                var query = _context.Libraries
                    .AsNoTracking()
                    .Join(
                        _context.UsersProfiles.AsNoTracking(),
                        l => l.UserId,
                        u => u.Id,
                        (l, u) => new { Library = l, User = u })
                    .Where(x =>
                        x.Library.ApprovalStatus != LibraryApprovalStatus.AwaitingEmailVerification &&
                        (x.Library.ApprovalStatus != LibraryApprovalStatus.Pending ||
                         x.Library.EmailVerifiedAtUtc.HasValue));

                if (status.HasValue)
                {
                    query = query.Where(x => x.Library.ApprovalStatus == status.Value);
                }

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var normalized = searchTerm.Trim();
                    query = query.Where(x =>
                        EF.Functions.ILike(x.Library.LibraryName, $"%{normalized}%") ||
                        EF.Functions.ILike(x.Library.Location, $"%{normalized}%") ||
                        EF.Functions.ILike(x.User.FirstName, $"%{normalized}%") ||
                        EF.Functions.ILike(x.User.LastName, $"%{normalized}%"));
                }

                var totalCount = await query.CountAsync(cancellationToken);

                var items = await query
                    .OrderByDescending(x => x.Library.CreationTime) // newest requests first
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(x => new LibraryRequestResponse(
                        x.Library.Id,
                        x.Library.LibraryName,
                        x.Library.Location,
                        x.Library.LibraryImage,
                        x.Library.HeaderImage,
                        x.Library.Email,
                        x.Library.EmailVerifiedAtUtc,
                        x.Library.ApprovalStatus,
                        x.Library.CreationTime,
                        new RequesterInfo(
                            x.User.Id,
                            x.User.FirstName,
                            x.User.LastName,
                            x.User.PhoneNumber)))
                    .ToListAsync(cancellationToken);

                return (items, totalCount);
            }

    public async Task<LibraryAggregate?> GetByIdAsync
            (Guid id, CancellationToken cancellationToken = default) => 
                await _context.Libraries.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        public async Task<LibraryAggregate?> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
                await _context.Libraries.FirstOrDefaultAsync(
                    library => library.UserId == userId,
                    cancellationToken);

        public async Task SaveChangesAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException(
                    "The library changed concurrently. Reload it and retry the operation.");
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
                "author" => sortDescending ? query.OrderByDescending(x => x.Author!.Name) : query.OrderBy(x => x.Author!.Name),
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
            public AuthorAggregate? Author { get; set; }
        }
    }
}
