using Quraaa.Application.Features.Books.Interfaces;
using Quraaa.Application.Shared.Files;

namespace Quraaa.API.Services
{
    public sealed class BulkBookStorageService : IBulkBookStorageService
    {
        // 80 KB — matches OS cluster size for efficient sequential writes.
        private const int BufferSize = 81_920;

        private readonly IWebHostEnvironment _environment;
        private readonly IFileStorageService _fileStorageService;

        public BulkBookStorageService(IWebHostEnvironment environment, IFileStorageService fileStorageService)
        {
            _environment = environment;
            _fileStorageService = fileStorageService;
        }

        // Cover images: publicly accessible for display in the frontend, so these stay
        // under wwwroot where UseStaticFiles can serve them directly.
        public Task<string> SaveCoverImageAsync(IUploadedFile file, CancellationToken cancellationToken = default)
            => SavePublicAsync(file, "uploads/books/covers", cancellationToken);

        // PDFs: stored under the private root (outside wwwroot) — never reachable via a direct URL.
        public Task<string> SavePdfAsync(IUploadedFile file, CancellationToken cancellationToken = default)
            => _fileStorageService.SaveAsync(file, "books/pdf", cancellationToken);

        // Word documents: stored alongside PDFs, also under the private root.
        public Task<string> SaveWordDocAsync(IUploadedFile file, CancellationToken cancellationToken = default)
            => _fileStorageService.SaveAsync(file, "books/docs", cancellationToken);

        public Task DeleteAsync(string? storedPath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(storedPath))
                return Task.CompletedTask;

            // Cover images are the only asset kind still rooted under wwwroot/uploads;
            // everything else lives under the private storage root.
            return storedPath.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase)
                ? DeletePublicAsync(storedPath)
                : _fileStorageService.DeleteAsync(storedPath, cancellationToken);
        }

        private Task DeletePublicAsync(string storedPath)
        {
            var root = GetPublicRootPath();
            var normalized = storedPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.GetFullPath(Path.Combine(root, normalized));

            // Guard against path-traversal: reject any path that escapes the web root.
            if (!fullPath.StartsWith(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase))
                return Task.CompletedTask;

            if (File.Exists(fullPath))
                File.Delete(fullPath);

            return Task.CompletedTask;
        }

        private async Task<string> SavePublicAsync(
            IUploadedFile file,
            string subPath,
            CancellationToken cancellationToken)
        {
            var root      = GetPublicRootPath();
            var directory = Path.Combine(root, subPath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(directory);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName  = $"{Guid.NewGuid():N}{extension}";
            var filePath  = Path.Combine(directory, fileName);

            await using var stream = new FileStream(
                filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                BufferSize, useAsync: true);

            await file.CopyToAsync(stream, cancellationToken);

            return $"{subPath}/{fileName}";
        }

        private string GetPublicRootPath() =>
            string.IsNullOrWhiteSpace(_environment.WebRootPath)
                ? Path.Combine(_environment.ContentRootPath, "wwwroot")
                : _environment.WebRootPath;
    }
}
