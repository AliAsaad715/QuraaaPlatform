using Quraaa.Application.Features.Books.Interfaces;
using Quraaa.Application.Shared.Files;

namespace Quraaa.API.Services
{
    public sealed class BulkBookStorageService : IBulkBookStorageService
    {
        private readonly IImageStorageService _imageStorageService;
        private readonly IFileStorageService _fileStorageService;

        public BulkBookStorageService(
            IImageStorageService imageStorageService,
            IFileStorageService fileStorageService)
        {
            _imageStorageService = imageStorageService;
            _fileStorageService = fileStorageService;
        }

        // Cover images are public display assets, but they must remain durable across
        // stateless host restarts and are therefore stored by the external image service.
        public Task<string> SaveCoverImageAsync(IUploadedFile file, CancellationToken cancellationToken = default)
            => _imageStorageService.UploadAsync(file, ImageAssetKind.BookCover, cancellationToken);

        // PDFs and Word documents are authenticated raw provider assets. Returned values
        // are opaque references, not URLs that a client can download directly.
        public Task<string> SavePdfAsync(IUploadedFile file, CancellationToken cancellationToken = default)
            => _fileStorageService.SaveAsync(file, "books/pdf", cancellationToken);

        public Task<string> SaveWordDocAsync(IUploadedFile file, CancellationToken cancellationToken = default)
            => _fileStorageService.SaveAsync(file, "books/docs", cancellationToken);

        public Task DeleteAsync(string? storedPath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(storedPath))
                return Task.CompletedTask;

            return Uri.TryCreate(storedPath, UriKind.Absolute, out var storedUri)
                && storedUri.Scheme == Uri.UriSchemeHttps
                && storedUri.AbsolutePath.Contains(
                    "/image/upload/",
                    StringComparison.Ordinal)
                ? _imageStorageService.DeleteAsync(storedPath, cancellationToken)
                : _fileStorageService.DeleteAsync(storedPath, cancellationToken);
        }
    }
}
