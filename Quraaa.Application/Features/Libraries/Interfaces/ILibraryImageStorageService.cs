using Quraaa.Application.Shared.Files;

namespace Quraaa.Application.Features.Libraries.Interfaces
{
    public interface ILibraryImageStorageService
    {
        Task<string> SaveAsync(IUploadedFile file, CancellationToken cancellationToken = default);
        Task DeleteAsync(string? storedPath, CancellationToken cancellationToken = default);
    }
}
