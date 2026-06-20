using Quraaa.Application.Features.Libraries.Commands.AddPhysicalBook;

namespace Quraaa.Application.Features.Libraries.Interfaces
{
    public interface IBookMetadataService
    {
        Task<BookMetadataDto?> GetBookByIsbnAsync(string isbn, CancellationToken cancellationToken = default);
    }
}
