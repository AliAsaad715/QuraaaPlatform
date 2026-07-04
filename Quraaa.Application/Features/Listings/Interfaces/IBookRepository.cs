using Quraaa.Domain.Catalog;

namespace Quraaa.Application.Features.Listings.Interfaces
{
    public interface IBookRepository
    {
        Task<BookAggregate?> FindByIsbnAsync(string isbn,
            CancellationToken cancellationToken = default);

        Task<BookAggregate?> FindByTitleAuthorLanguageAsync(
            string title, string author, string language,
            CancellationToken cancellationToken = default);

        Task AddAsync(BookAggregate book,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}