using Quraaa.Application.Shared.Files;

namespace Quraaa.Application.Features.Books.Interfaces
{
    public interface IBulkBookStorageService
    {
        /// <summary>Saves a cover image and returns its relative URL path.</summary>
        Task<string> SaveCoverImageAsync(IUploadedFile file, CancellationToken cancellationToken = default);

        /// <summary>Saves a PDF file and returns its relative storage path.</summary>
        Task<string> SavePdfAsync(IUploadedFile file, CancellationToken cancellationToken = default);

        /// <summary>Saves a Word document and returns its relative storage path.</summary>
        Task<string> SaveWordDocAsync(IUploadedFile file, CancellationToken cancellationToken = default);

        /// <summary>Deletes a file by its stored relative path. No-ops silently if the file does not exist.</summary>
        Task DeleteAsync(string? storedPath, CancellationToken cancellationToken = default);
    }
}
