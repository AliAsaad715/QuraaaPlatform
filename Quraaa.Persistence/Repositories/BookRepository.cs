using Microsoft.EntityFrameworkCore;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Domain.Catalog;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories
{
    public class BookRepository : IBookRepository
    {
        private readonly ApplicationDbContext _context;

        public BookRepository(ApplicationDbContext context) => _context = context;

        public async Task<BookAggregate?> FindByIsbnAsync(
            string isbn, CancellationToken cancellationToken = default) =>
            await _context.Books
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Isbn == isbn, cancellationToken);

        public async Task<BookAggregate?> FindByTitleAuthorLanguageAsync(
            string title, string author, string language,
            CancellationToken cancellationToken = default) =>
            await _context.Books
                .AsNoTracking()
                .FirstOrDefaultAsync(b =>
                    EF.Functions.ILike(b.Title, title) &&
                    EF.Functions.ILike(b.Author, author) &&
                    EF.Functions.ILike(b.Language, language),
                    cancellationToken);

        public async Task AddAsync(
            BookAggregate book, CancellationToken cancellationToken = default) =>
            await _context.Books.AddAsync(book, cancellationToken);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            _context.SaveChangesAsync(cancellationToken);
    }
}