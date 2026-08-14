using Quraaa.Application.Shared.Files;

namespace Quraaa.Application.Features.Libraries.Interfaces
{
    public interface ILibraryImageStorageService
    {
        Task<string> SaveLibraryImageAsync(
            IUploadedFile file,
            CancellationToken cancellationToken = default);

        Task<string> SaveHeaderImageAsync(
            IUploadedFile file,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(string? storedPath, CancellationToken cancellationToken = default);
    }
}
