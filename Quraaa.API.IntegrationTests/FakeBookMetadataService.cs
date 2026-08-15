using Quraaa.Application.Features.Listings.Commands.AddPhysicalBook;
using Quraaa.Application.Features.Listings.Interfaces;

namespace Quraaa.API.IntegrationTests
{
    /// <summary>
    /// Stands in for the real Google Books client so tests control exactly which ISBNs
    /// "exist" without depending on network access or Google's actual catalog.
    /// </summary>
    public sealed class FakeBookMetadataService : IBookMetadataService
    {
        private readonly Dictionary<string, BookMetadataDto> _knownIsbns = new(StringComparer.Ordinal);

        public void SetResult(string isbn, BookMetadataDto? metadata)
        {
            if (metadata is null)
            {
                _knownIsbns.Remove(isbn);
            }
            else
            {
                _knownIsbns[isbn] = metadata;
            }
        }

        public Task<BookMetadataDto?> GetBookByIsbnAsync(string isbn, CancellationToken cancellationToken = default)
        {
            var metadata = _knownIsbns.TryGetValue(isbn, out var value) ? value : null;
            return Task.FromResult(metadata);
        }
    }
}
