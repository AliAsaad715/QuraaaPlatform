using Quraaa.Application.Shared.Files;

namespace Quraaa.Application.Features.Books.Interfaces
{
    public interface IBulkBookStorageService
    {
        /// <summary>Saves a cover image and returns its absolute HTTPS delivery URL.</summary>
        Task<string> SaveCoverImageAsync(IUploadedFile file, CancellationToken cancellationToken = default);

        /// <summary>Saves a private PDF and returns its opaque storage reference.</summary>
        Task<string> SavePdfAsync(IUploadedFile file, CancellationToken cancellationToken = default);

        /// <summary>Saves a private Word document and returns its opaque storage reference.</summary>
        Task<string> SaveWordDocAsync(IUploadedFile file, CancellationToken cancellationToken = default);

        /// <summary>Deletes an owned image URL or private file reference.</summary>
        Task DeleteAsync(string? storedPath, CancellationToken cancellationToken = default);
    }
}
