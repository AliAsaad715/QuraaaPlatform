using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Shared.Files;

namespace Quraaa.API.Services
{
    public class LibraryImageStorageService : ILibraryImageStorageService
    {
        private readonly IWebHostEnvironment _environment;

        public LibraryImageStorageService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> SaveAsync(IUploadedFile file, CancellationToken cancellationToken = default)
        {
            var uploadDirectory = Path.Combine(GetWebRootPath(), "uploads", "libraries");
            Directory.CreateDirectory(uploadDirectory);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadDirectory, fileName);

            await using var stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await file.CopyToAsync(stream, cancellationToken);

            return $"/uploads/libraries/{fileName}";
        }

        public Task DeleteAsync(string? storedPath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(storedPath))
            {
                return Task.CompletedTask;
            }

            var normalizedPath = storedPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.GetFullPath(Path.Combine(GetWebRootPath(), normalizedPath));
            var webRootFullPath = Path.GetFullPath(GetWebRootPath());

            if (!fullPath.StartsWith(webRootFullPath, StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            return Task.CompletedTask;
        }

        private string GetWebRootPath()
        {
            return string.IsNullOrWhiteSpace(_environment.WebRootPath)
                ? Path.Combine(_environment.ContentRootPath, "wwwroot")
                : _environment.WebRootPath;
        }
    }
}
