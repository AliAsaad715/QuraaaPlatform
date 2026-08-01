using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Shared.Files;

namespace Quraaa.API.Services
{
    public class LibraryBookStorageService : ILibraryBookStorageService
    {
        private readonly IWebHostEnvironment _environment;

        public LibraryBookStorageService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> SaveAsync(IUploadedFile file, CancellationToken cancellationToken = default)
        {
            var uploadDirectory = Path.Combine(GetStorageRootPath(), "books");
            Directory.CreateDirectory(uploadDirectory);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadDirectory, fileName);

            await using var stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await file.CopyToAsync(stream, cancellationToken);

            return $"books/{fileName}";
        }

        private string GetStorageRootPath()
        {
            return Path.Combine(
                string.IsNullOrWhiteSpace(_environment.WebRootPath)
                    ? _environment.ContentRootPath
                    : _environment.WebRootPath,
                "storage");
        }
    }
}