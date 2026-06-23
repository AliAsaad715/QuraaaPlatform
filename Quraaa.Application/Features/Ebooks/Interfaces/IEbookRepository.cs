using Quraaa.Application.Features.Ebooks.Common;

namespace Quraaa.Application.Features.Ebooks.Interfaces
{
    public interface IEbookRepository
    {
        Task<(IReadOnlyCollection<EbookResponse> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm,
            CancellationToken cancellationToken = default);
    }
}
