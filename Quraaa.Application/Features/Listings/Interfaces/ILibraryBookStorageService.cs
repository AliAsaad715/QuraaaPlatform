using Quraaa.Application.Shared.Files;

namespace Quraaa.Application.Features.Listings.Interfaces
{
    public interface ILibraryBookStorageService
    {
        Task<string> SaveAsync(IUploadedFile file, CancellationToken cancellationToken = default);

        Task DeleteAsync(string? storedReference, CancellationToken cancellationToken = default);
    }
}
