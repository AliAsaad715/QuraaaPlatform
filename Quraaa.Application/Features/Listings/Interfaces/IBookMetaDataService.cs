using Quraaa.Application.Features.Listings.Commands.AddPhysicalBook;

namespace Quraaa.Application.Features.Listings.Interfaces
{
    public interface IBookMetadataService
    {
        Task<BookMetadataDto?> GetBookByIsbnAsync(string isbn, CancellationToken cancellationToken = default);
    }
}
